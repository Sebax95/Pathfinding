// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace AmplifyShaderEditor
{
	public static class MaterialPropertyHandlerEx
	{
		private static System.Type type = null;
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEditor.MaterialPropertyHandler, UnityEditor" ) : type; } }
		public static object GetHandler( Shader shader, string name )
		{
			return MaterialPropertyHandlerEx.Type.InvokeMember( "GetHandler", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { shader, name } );
		}

		public static void OnGUI( object obj, ref Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor )
		{
			Type.InvokeMember( "OnGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, obj, new object[] { position, prop, label, editor } );
		}

		public static float GetPropertyHeight( object obj, MaterialProperty prop, string label, MaterialEditor editor )
		{
			return (float)Type.InvokeMember( "GetPropertyHeight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, obj, new object[] { prop, label, editor } );
		}

		public static object PropertyDrawer( object obj )
		{
			return Type.InvokeMember( "propertyDrawer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, obj, new object[] {} );
		}
	}

	internal class MaterialInspector : ShaderGUI
	{
		private const string CopyButtonStr = "Copy Values";
		private const string PasteButtonStr = "Paste Values";
		private const string PreviewModelPref = "ASEMI_PREVIEWMODEL";

		private static MaterialEditor m_instance = null;
		private static bool m_refreshOnUndo = false;

		private bool m_initialized = false;
		private double m_lastRenderedTime;
		private PreviewRenderUtility m_previewRenderUtility;
		private Mesh m_targetMesh;
		private Vector2 m_previewDir = new Vector2( 120f, -20f );
		private int m_selectedMesh = 0;
		private int m_prevSelectedMesh = 0;


		// Reflection Fields

		private FieldInfo m_previewDirDefault = null;

		private Type m_modelInspectorType = null;
		private MethodInfo m_renderMeshMethod = null;
		private Type m_previewGUIType = null;
		private MethodInfo m_dragMethod = null;
		private FieldInfo m_selectedField = null;
		private FieldInfo m_infoField = null;

	#if UNITY_2020_1_OR_NEWER
		private Type m_previewSettingsType = null;
		object m_previewSettingsInstance;
		FieldInfo previewDirInfo;
		FieldInfo shadedMaterialInfo;
		FieldInfo activeMaterialInfo;
	#endif

		public override void OnClosed( Material material )
		{
			base.OnClosed( material );
			CleanUp();
		}

		void CleanUp()
		{
			if( m_previewRenderUtility != null )
			{
				m_previewRenderUtility.Cleanup();
				m_previewRenderUtility = null;
			}
		}

		void UndoRedoPerformed()
		{
			m_refreshOnUndo = true;
		}

		~MaterialInspector()
		{
			UndoUtils.UnregisterUndoRedoCallback( UndoRedoPerformed );
			CleanUp();
		}
		public override void OnGUI( MaterialEditor materialEditor, MaterialProperty[] properties )
		{
			IOUtils.Init();
			Material mat = materialEditor.target as Material;

			if( mat == null )
				return;

			m_instance = materialEditor;

			if( !m_initialized )
			{
				Init();
				m_initialized = true;
				UndoUtils.RegisterUndoRedoCallback( UndoRedoPerformed );
			}

			if( Event.current.type == EventType.Repaint &&
				mat.HasProperty( IOUtils.DefaultASEDirtyCheckId ) &&
				mat.GetInt( IOUtils.DefaultASEDirtyCheckId ) == 1 )
			{
				mat.SetInt( IOUtils.DefaultASEDirtyCheckId, 0 );
				UIUtils.ForceUpdateFromMaterial();
				//Event.current.Use();
			}

			if( materialEditor.isVisible )
			{
				GUILayout.BeginVertical();
				{
					GUILayout.Space( 3 );
					if( GUILayout.Button( "Open in Shader Editor" ) )
					{
						ASEPackageManagerHelper.SetupLateMaterial( mat );
					}

					GUILayout.BeginHorizontal();
					{
						if( GUILayout.Button( CopyButtonStr ) )
						{
							System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

							Shader shader = mat.shader;
							int propertyCount = shader.GetPropertyCount();
							string allProperties = string.Empty;
							for( int i = 0; i < propertyCount; i++ )
							{
								UnityEngine.Rendering.ShaderPropertyType type = shader.GetPropertyType( i );
								string name = shader.GetPropertyName( i );
								string valueStr = string.Empty;
								switch( type )
								{
									case UnityEngine.Rendering.ShaderPropertyType.Color:
									{
										Color value = mat.GetColor( name );
										valueStr = value.r.ToString() + IOUtils.VECTOR_SEPARATOR +
													value.g.ToString() + IOUtils.VECTOR_SEPARATOR +
													value.b.ToString() + IOUtils.VECTOR_SEPARATOR +
													value.a.ToString();
									}
									break;
									case UnityEngine.Rendering.ShaderPropertyType.Vector:
									{
										Vector4 value = mat.GetVector( name );
										valueStr = value.x.ToString() + IOUtils.VECTOR_SEPARATOR +
													value.y.ToString() + IOUtils.VECTOR_SEPARATOR +
													value.z.ToString() + IOUtils.VECTOR_SEPARATOR +
													value.w.ToString();
									}
									break;
									case UnityEngine.Rendering.ShaderPropertyType.Float:
									{
										float value = mat.GetFloat( name );
										valueStr = value.ToString();
									}
									break;
									case UnityEngine.Rendering.ShaderPropertyType.Range:
									{
										float value = mat.GetFloat( name );
										valueStr = value.ToString();
									}
									break;
									case UnityEngine.Rendering.ShaderPropertyType.Texture:
									{
										Texture value = mat.GetTexture( name );
										valueStr = AssetDatabase.GetAssetPath( value );
										Vector2 offset = mat.GetTextureOffset( name );
										Vector2 scale = mat.GetTextureScale( name );
										valueStr += IOUtils.VECTOR_SEPARATOR + scale.x.ToString() +
											IOUtils.VECTOR_SEPARATOR + scale.y.ToString() +
											IOUtils.VECTOR_SEPARATOR + offset.x.ToString() +
											IOUtils.VECTOR_SEPARATOR + offset.y.ToString();
									}
									break;
								}

								allProperties += name + IOUtils.FIELD_SEPARATOR + type + IOUtils.FIELD_SEPARATOR + valueStr;

								if( i < ( propertyCount - 1 ) )
								{
									allProperties += IOUtils.LINE_TERMINATOR;
								}
							}
							EditorPrefs.SetString( IOUtils.MAT_CLIPBOARD_ID, allProperties );
							System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
						}

						if( GUILayout.Button( PasteButtonStr ) )
						{
							System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
							string propertiesStr = EditorPrefs.GetString( IOUtils.MAT_CLIPBOARD_ID, string.Empty );
							if( !string.IsNullOrEmpty( propertiesStr ) )
							{
								string[] propertyArr = propertiesStr.Split( IOUtils.LINE_TERMINATOR );
								bool validData = true;
								try
								{
									for( int i = 0; i < propertyArr.Length; i++ )
									{
										string[] valuesArr = propertyArr[ i ].Split( IOUtils.FIELD_SEPARATOR );
										if( valuesArr.Length != 3 )
										{
											Debug.LogWarning( "Material clipboard data is corrupted" );
											validData = false;
											break;
										}
										else if( mat.HasProperty( valuesArr[ 0 ] ) )
										{
											UnityEngine.Rendering.ShaderPropertyType type = (UnityEngine.Rendering.ShaderPropertyType)Enum.Parse( typeof( UnityEngine.Rendering.ShaderPropertyType ), valuesArr[ 1 ] );
											switch( type )
											{
												case UnityEngine.Rendering.ShaderPropertyType.Color:
												{
													string[] colorVals = valuesArr[ 2 ].Split( IOUtils.VECTOR_SEPARATOR );
													if( colorVals.Length != 4 )
													{
														Debug.LogWarning( "Material clipboard data is corrupted" );
														validData = false;
														break;
													}
													else
													{
														mat.SetColor( valuesArr[ 0 ], new Color( Convert.ToSingle( colorVals[ 0 ] ),
																									Convert.ToSingle( colorVals[ 1 ] ),
																									Convert.ToSingle( colorVals[ 2 ] ),
																									Convert.ToSingle( colorVals[ 3 ] ) ) );
													}
												}
												break;
												case UnityEngine.Rendering.ShaderPropertyType.Vector:
												{
													string[] vectorVals = valuesArr[ 2 ].Split( IOUtils.VECTOR_SEPARATOR );
													if( vectorVals.Length != 4 )
													{
														Debug.LogWarning( "Material clipboard data is corrupted" );
														validData = false;
														break;
													}
													else
													{
														mat.SetVector( valuesArr[ 0 ], new Vector4( Convert.ToSingle( vectorVals[ 0 ] ),
																									Convert.ToSingle( vectorVals[ 1 ] ),
																									Convert.ToSingle( vectorVals[ 2 ] ),
																									Convert.ToSingle( vectorVals[ 3 ] ) ) );
													}
												}
												break;
												case UnityEngine.Rendering.ShaderPropertyType.Float:
												{
													mat.SetFloat( valuesArr[ 0 ], Convert.ToSingle( valuesArr[ 2 ] ) );
												}
												break;
												case UnityEngine.Rendering.ShaderPropertyType.Range:
												{
													mat.SetFloat( valuesArr[ 0 ], Convert.ToSingle( valuesArr[ 2 ] ) );
												}
												break;
												case UnityEngine.Rendering.ShaderPropertyType.Texture:
												{
													string[] texVals = valuesArr[ 2 ].Split( IOUtils.VECTOR_SEPARATOR );
													if( texVals.Length != 5 )
													{
														Debug.LogWarning( "Material clipboard data is corrupted" );
														validData = false;
														break;
													}
													else
													{
														mat.SetTexture( valuesArr[ 0 ], AssetDatabase.LoadAssetAtPath<Texture>( texVals[ 0 ] ) );
														mat.SetTextureScale( valuesArr[ 0 ], new Vector2( Convert.ToSingle( texVals[ 1 ] ), Convert.ToSingle( texVals[ 2 ] ) ) );
														mat.SetTextureOffset( valuesArr[ 0 ], new Vector2( Convert.ToSingle( texVals[ 3 ] ), Convert.ToSingle( texVals[ 4 ] ) ) );
													}
												}
												break;
											}
										}
									}
								}
								catch( Exception e )
								{
									Debug.LogException( e );
									validData = false;
								}


								if( validData )
								{
									materialEditor.PropertiesChanged();
									UIUtils.CopyValuesFromMaterial( mat );
								}
								else
								{
									EditorPrefs.SetString( IOUtils.MAT_CLIPBOARD_ID, string.Empty );
								}
							}
							System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
						}
					}
					GUILayout.EndHorizontal();
					GUILayout.Space( 5 );
				}
				GUILayout.EndVertical();
			}
			EditorGUI.BeginChangeCheck();
			//base.OnGUI( materialEditor, properties );

			// Draw custom properties instead of calling BASE to use single line texture properties
			materialEditor.SetDefaultGUIWidths();

			if( m_infoField == null )
			{
				m_infoField = typeof( MaterialEditor ).GetField( "m_InfoMessage", BindingFlags.Instance | BindingFlags.NonPublic );
			}

			string info = m_infoField.GetValue( materialEditor ) as string;
			if( !string.IsNullOrEmpty( info ) )
			{
				EditorGUILayout.HelpBox( info, MessageType.Info );
			}
			else
			{
				GUIUtility.GetControlID( "EditorTextField".GetHashCode(), FocusType.Passive, new Rect( 0f, 0f, 0f, 0f ) );
			}

			for( int i = 0; i < properties.Length; i++ )
			{
			#if UNITY_6000_2_OR_NEWER
				int propertyFlags = ( int )properties[ i ].propertyFlags;
			#else
				int propertyFlags = ( int )properties[ i ].flags;
			#endif

				if ( ( propertyFlags & ( ( int )UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector | ( int )UnityEngine.Rendering.ShaderPropertyFlags.PerRendererData ) ) == ( int )UnityEngine.Rendering.ShaderPropertyFlags.None )
				{
					// Removed no scale offset one line texture property for consistency :( sad face
					//if( ( properties[ i ].flags & MaterialProperty.PropFlags.NoScaleOffset ) == MaterialProperty.PropFlags.NoScaleOffset )
					//{
					//	object obj = MaterialPropertyHandlerEx.GetHandler( mat.shader, properties[ i ].name );
					//	if( obj != null )
					//	{
					//		float height = MaterialPropertyHandlerEx.GetPropertyHeight( obj, properties[ i ], properties[ i ].displayName, materialEditor );
					//		//Rect rect = (Rect)materialEditor.GetType().InvokeMember( "GetPropertyRect", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, materialEditor, new object[] { properties[ i ], properties[ i ].displayName, true } );
					//		Rect rect = EditorGUILayout.GetControlRect( true, height, EditorStyles.layerMaskField );
					//		MaterialPropertyHandlerEx.OnGUI( obj, ref rect, properties[ i ], new GUIContent( properties[ i ].displayName ), materialEditor );

					//		if( MaterialPropertyHandlerEx.PropertyDrawer( obj ) != null )
					//			continue;

					//		rect = EditorGUILayout.GetControlRect( true, height, EditorStyles.layerMaskField );
					//		materialEditor.TexturePropertyMiniThumbnail( rect, properties[ i ], properties[ i ].displayName, string.Empty );
					//	}
					//	else
					//	{
					//		materialEditor.TexturePropertySingleLine( new GUIContent( properties[ i ].displayName ), properties[ i ] );
					//	}
					//}
					//else
					//{
						float propertyHeight = materialEditor.GetPropertyHeight( properties[ i ], properties[ i ].displayName );
						Rect controlRect = EditorGUILayout.GetControlRect( true, propertyHeight, EditorStyles.layerMaskField, new GUILayoutOption[ 0 ] );
						materialEditor.ShaderProperty( controlRect, properties[ i ], properties[ i ].displayName );
					//}
				}
			}

			EditorGUILayout.Space();
			materialEditor.RenderQueueField();
			materialEditor.EnableInstancingField();
			materialEditor.DoubleSidedGIField();
			materialEditor.LightmapEmissionProperty();
			if( m_refreshOnUndo || EditorGUI.EndChangeCheck() )
			{
				m_refreshOnUndo = false;

				string isEmissive = mat.GetTag( "IsEmissive", false, "false" );
				if( isEmissive.Equals( "true" ) )
				{
					mat.globalIlluminationFlags &= (MaterialGlobalIlluminationFlags)3;
				}
				else
				{
					mat.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
				}

				UIUtils.CopyValuesFromMaterial( mat );
			}

			if( materialEditor.RequiresConstantRepaint() && m_lastRenderedTime + 0.032999999821186066 < EditorApplication.timeSinceStartup )
			{
				this.m_lastRenderedTime = EditorApplication.timeSinceStartup;
				materialEditor.Repaint();
			}
		}

		private void Init()
		{
			string guid = EditorPrefs.GetString( PreviewModelPref, "" );
			if( !string.IsNullOrEmpty( guid ) )
			{
				m_targetMesh = AssetDatabase.LoadAssetAtPath<Mesh>( AssetDatabase.GUIDToAssetPath( guid ) );
			}
		}

		public override void OnMaterialPreviewSettingsGUI( MaterialEditor materialEditor )
		{

			base.OnMaterialPreviewSettingsGUI( materialEditor );

			if( UnityEditor.ShaderUtil.hardwareSupportsRectRenderTexture )
			{
				EditorGUI.BeginChangeCheck();
				m_targetMesh = (Mesh)EditorGUILayout.ObjectField( m_targetMesh, typeof( Mesh ), false, GUILayout.MaxWidth( 120 ) );
				if( EditorGUI.EndChangeCheck() )
				{
					if( m_targetMesh != null )
					{
						EditorPrefs.SetString( PreviewModelPref, AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_targetMesh ) ) );
					}
					else
					{
						EditorPrefs.SetString( PreviewModelPref, "" );
					}
				}

				if( m_selectedField == null )
				{
					m_selectedField = typeof( MaterialEditor ).GetField( "m_SelectedMesh", BindingFlags.Instance | BindingFlags.NonPublic );
				}

				if( m_previewDirDefault == null )
				{
					m_previewDirDefault = typeof( MaterialEditor ).GetField( "m_PreviewDir" , BindingFlags.Instance | BindingFlags.NonPublic );
				}


				m_selectedMesh = (int)m_selectedField.GetValue( materialEditor );
				if ( m_selectedMesh != m_prevSelectedMesh )
				{
					m_prevSelectedMesh = m_selectedMesh;
					if( m_targetMesh != null )
					{
						m_targetMesh = null;
						EditorPrefs.SetString( PreviewModelPref, "" );
					}
				}
			}

			if( GUILayout.Button( "R" ,GUILayout.MaxWidth(17), GUILayout.MaxHeight( 13 ) ) )
			{
				m_previewDir = new Vector2( 0 , 0 );
				if( m_previewDirDefault != null )
					m_previewDirDefault.SetValue( materialEditor , m_previewDir );
			}
		}

		public override void OnMaterialInteractivePreviewGUI( MaterialEditor materialEditor, Rect r, GUIStyle background )
		{
			if( Event.current.type == EventType.DragExited )
			{
				if( DragAndDrop.objectReferences.Length > 0 )
				{
					GameObject dropped = DragAndDrop.objectReferences[ 0 ] as GameObject;
					if( dropped != null )
					{
						m_targetMesh = AssetDatabase.LoadAssetAtPath<Mesh>( AssetDatabase.GetAssetPath( dropped ) );
						EditorPrefs.SetString( PreviewModelPref, AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_targetMesh ) ) );
					}
				}
			}

			if( m_targetMesh == null )
			{
				base.OnMaterialInteractivePreviewGUI( materialEditor, r, background );
				return;
			}

			Material mat = materialEditor.target as Material;

			if( m_previewRenderUtility == null )
			{
				m_previewRenderUtility = new PreviewRenderUtility();
				m_previewRenderUtility.cameraFieldOfView = 30f;
			}

			if( m_previewGUIType == null )
			{
				m_previewGUIType = Type.GetType( "PreviewGUI, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" );
				m_dragMethod = m_previewGUIType.GetMethod( "Drag2D", BindingFlags.Static | BindingFlags.Public );
			}

			m_previewDir = ( Vector2 )m_dragMethod.Invoke( m_previewGUIType, new object[] { m_previewDir, r } );

			if ( m_modelInspectorType == null )
			{
				m_modelInspectorType = Type.GetType( "UnityEditor.MeshPreview, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" );
				if ( m_modelInspectorType == null )
				{
					m_modelInspectorType = Type.GetType( "UnityEditor.ModelInspector, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" );
				}
				m_renderMeshMethod = m_modelInspectorType.GetMethod( "RenderMeshPreview", BindingFlags.Static | BindingFlags.NonPublic );
			}

	#if UNITY_2020_1_OR_NEWER

			m_previewDir = ( Vector2 )m_dragMethod.Invoke( m_previewGUIType, new object[] { m_previewDir, r } );

			if ( m_previewSettingsType == null )
			{
				m_previewSettingsType = m_modelInspectorType.GetNestedType( "PreviewSettings", BindingFlags.NonPublic );
				if ( m_previewSettingsType == null )
				{
					m_previewSettingsType = m_modelInspectorType.GetNestedType( "Settings", BindingFlags.NonPublic );
				}
			}

			if ( m_previewSettingsInstance == null )
			{
				m_previewSettingsInstance = Activator.CreateInstance( m_previewSettingsType );
			}

			if ( shadedMaterialInfo == null || activeMaterialInfo == null || previewDirInfo == null )
			{
				shadedMaterialInfo = m_previewSettingsType.GetField( "shadedPreviewMaterial", BindingFlags.Instance | BindingFlags.Public );
				activeMaterialInfo = m_previewSettingsType.GetField( "activeMaterial", BindingFlags.Instance | BindingFlags.Public );
				previewDirInfo = m_previewSettingsType.GetField( "previewDir", BindingFlags.Instance | BindingFlags.Public );
			}

			if ( shadedMaterialInfo == null || activeMaterialInfo == null || previewDirInfo == null )
			{
				shadedMaterialInfo = m_previewSettingsType.GetField( "m_ShadedPreviewMaterial", BindingFlags.Instance | BindingFlags.NonPublic );
				activeMaterialInfo = m_previewSettingsType.GetField( "m_ActiveMaterial", BindingFlags.Instance | BindingFlags.NonPublic );
				previewDirInfo = m_previewSettingsType.GetField( "m_PreviewDir", BindingFlags.Instance | BindingFlags.NonPublic );
			}

			shadedMaterialInfo.SetValue( m_previewSettingsInstance, mat );
			activeMaterialInfo.SetValue( m_previewSettingsInstance, mat );
			previewDirInfo.SetValue( m_previewSettingsInstance, m_previewDir );

			if ( Event.current.type == EventType.Repaint )
			{
				m_previewRenderUtility.BeginPreview( r, background );
				m_renderMeshMethod.Invoke( m_modelInspectorType, new object[] { m_targetMesh, m_previewRenderUtility, m_previewSettingsInstance, -1 } );
				m_previewRenderUtility.EndAndDrawPreview( r );
			}

	#else

			if( Event.current.type == EventType.Repaint )
			{
				m_previewRenderUtility.BeginPreview( r, background );
				m_renderMeshMethod.Invoke( m_modelInspectorType, new object[] { m_targetMesh, m_previewRenderUtility, mat, null, m_previewDir, -1 } );
				m_previewRenderUtility.EndAndDrawPreview( r );
			}

	#endif
		}

		public static MaterialEditor Instance { get { return m_instance; } set { m_instance = value; } }
	}
}