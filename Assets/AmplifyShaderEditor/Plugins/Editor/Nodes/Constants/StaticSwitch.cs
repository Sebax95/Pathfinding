// Amplify Shader Editor - Visual Shader vEditing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch", "Logical Operators", "Creates a shader keyword toggle", Available = true )]
	public sealed class StaticSwitch : PropertyNode
	{
		public enum ShaderStage
		{
			All,
			Vertex,
			Fragment,
			Hull,
			Domain,
			Geometry,
			Raytracing
		};

		private float InstanceIconWidth = 19;
		private float InstanceIconHeight = 19;
		private readonly Color ReferenceHeaderColor = new Color( 0f, 0.5f, 0.585f, 1.0f );

		[SerializeField]
		private int m_defaultValue = 0;

		[SerializeField]
		private int m_materialValue = 0;

		[SerializeField]
		private int m_multiCompile = 0;

		[SerializeField]
		private int m_currentKeywordId = 0;

		[SerializeField]
		private string m_currentKeyword = string.Empty;

		[SerializeField]
		private bool m_createToggle = true;

		[SerializeField]
		private bool m_lockKeyword = true;

		private const string IsLocalStr = "Is Local";
		private const string StageStr = "Stage";

		[SerializeField]
		private bool m_isLocal = true;

		[SerializeField]
		private ShaderStage m_shaderStage = ShaderStage.All;


		private GUIContent m_checkContent;
		private GUIContent m_popContent;

		private int m_conditionId = -1;

		private const int MinComboSize = 50;
		private const int MaxComboSize = 105;

		private Rect m_varRect;
		private Rect m_imgRect;
		private bool m_editing;

		public enum KeywordModeType
		{
			Toggle = 0,
			ToggleOff = 1,
			KeywordEnum = 2,
		}

		public enum StaticSwitchVariableMode
		{
			Create = 0,
			Fetch = 1,
			Reference = 2
		}

		public enum KeywordType
		{
			ShaderFeature = 0,
			MultiCompile = 1,
			DynamicBranch = 2,
		}

		[SerializeField]
		private KeywordModeType m_keywordModeType = KeywordModeType.Toggle;

		[SerializeField]
		private StaticSwitch m_reference = null;

		private const string StaticSwitchStr = "Switch";
		private const string MaterialToggleStr = "Material Toggle";

		private const string ToggleMaterialValueStr = "Material Value";
		private const string ToggleDefaultValueStr = "Default Value";

		private const string AmountStr = "Amount";
		private const string KeywordStr = "Keyword";
		private const string CustomStr = "Custom";
		private const string ToggleTypeStr = "Toggle Type";
		private const string TypeStr = "Type";
		private const string ModeStr = "Mode";
		private const string KeywordTypeStr = "Keyword Type";

		private const string KeywordNameStr = "Keyword Name";
		public readonly static string[] KeywordTypeList = { "Shader Feature", "Multi Compile", "Dynamic Branch" };
		public readonly static int[] KeywordTypeInt = { ( int )KeywordType.ShaderFeature, ( int )KeywordType.MultiCompile, ( int )KeywordType.DynamicBranch };

		[SerializeField]
		private string[] m_defaultKeywordNames = { "Key0", "Key1", "Key2", "Key3", "Key4", "Key5", "Key6", "Key7", "Key8" };

		[SerializeField]
		private string[] m_keywordEnumList = { "Key0", "Key1" };

		[SerializeField]
		private StaticSwitchVariableMode m_staticSwitchVarMode = StaticSwitchVariableMode.Create;

		[SerializeField]
		private int m_referenceArrayId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		private int m_keywordEnumAmount = 2;

		private bool m_isStaticSwitchDirty = false;

		private Rect m_iconPos;

		public const string DynamicBranchErrorMsg = "dynamic_branch requires Unity 2022.1+";
		public const string KeywordModifierSurfaceWarning = "Keyword stage ignored in Surface Shaders due to Unity bug.";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			AddInputPort( WirePortDataType.FLOAT, false, "False", -1, MasterNodePortCategory.Fragment, 1 );
			AddInputPort( WirePortDataType.FLOAT, false, "True", -1, MasterNodePortCategory.Fragment, 0 );
			for( int i = 2; i < 9; i++ )
			{
				AddInputPort( WirePortDataType.FLOAT, false, m_defaultKeywordNames[ i ] );
				m_inputPorts[ i ].Visible = false;
			}
			m_headerColor = new Color( 0.0f, 0.55f, 0.45f, 1f );
			m_customPrefix = KeywordStr + " ";
			m_autoWrapProperties = false;
			m_freeType = false;
			m_useVarSubtitle = true;
			m_allowPropertyDuplicates = true;
			m_showTitleWhenNotEditing = false;
			m_currentParameterType = PropertyType.Property;

			m_checkContent = new GUIContent();
			m_checkContent.image = UIUtils.CheckmarkIcon;

			m_popContent = new GUIContent();
			m_popContent.image = UIUtils.PopupIcon;

			m_previewShaderGUID = "0b708c11c68e6a9478ac97fe3643eab1";
			m_showAutoRegisterUI = true;

			m_errorMessageTooltip = DynamicBranchErrorMsg;
			m_errorMessageTypeIsError = NodeMessageType.Error;
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_conditionId == -1 )
				m_conditionId = Shader.PropertyToID( "_Condition" );

			StaticSwitch node = ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

			if ( m_createToggle && m_materialMode )
				PreviewMaterial.SetInt( m_conditionId, node.MaterialValue );
			else
				PreviewMaterial.SetInt( m_conditionId, node.DefaultValue );
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();

			if( m_createToggle )
				UIUtils.RegisterPropertyNode( this );
			else
				UIUtils.UnregisterPropertyNode( this );

			if( CurrentVarMode != StaticSwitchVariableMode.Reference )
			{
				ContainerGraph.StaticSwitchNodes.AddNode( this );
			}

			if( UniqueId > -1 )
				ContainerGraph.StaticSwitchNodes.OnReorderEventComplete += OnReorderEventComplete;
		}

		public override void Destroy()
		{
			base.Destroy();
			UIUtils.UnregisterPropertyNode( this );
			if( CurrentVarMode != StaticSwitchVariableMode.Reference )
			{
				ContainerGraph.StaticSwitchNodes.RemoveNode( this );
			}

			if( UniqueId > -1 )
				ContainerGraph.StaticSwitchNodes.OnReorderEventComplete -= OnReorderEventComplete;
		}

		void OnReorderEventComplete()
		{
			if( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				if( m_reference != null )
				{
					m_referenceArrayId = ContainerGraph.StaticSwitchNodes.GetNodeRegisterIdx( m_reference.UniqueId );
				}
			}
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnections();
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnections();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnections();
		}

		private void UpdateConnections()
		{
			WirePortDataType mainType = WirePortDataType.FLOAT;

			int highest = UIUtils.GetPriority( mainType );
			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if( m_inputPorts[ i ].IsConnected )
				{
					WirePortDataType portType = m_inputPorts[ i ].GetOutputConnection().DataType;
					if( UIUtils.GetPriority( portType ) > highest )
					{
						mainType = portType;
						highest = UIUtils.GetPriority( portType );
					}
				}
			}

			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].ChangeType( mainType, false );
			}

			m_outputPorts[ 0 ].ChangeType( mainType, false );
		}

		public override string GetPropertyValue()
		{
			if( m_createToggle )
			{
				string value = UIUtils.PropertyFloatToString( m_defaultValue );
				if ( m_keywordModeType == KeywordModeType.KeywordEnum && m_keywordEnumAmount > 0 )
				{
					return PropertyAttributes + "[" + m_keywordModeType.ToString() + "( " + GetKeywordEnumPropertyList() + " )] " + m_propertyName + "( \"" + m_propertyInspectorName + "\", Float ) = " + value;
				}
				else
				{
					return PropertyAttributes + "[" + m_keywordModeType.ToString() + "( " + GetPropertyValStr() + " )] " + m_propertyName + "( \"" + m_propertyInspectorName + "\", Float ) = " + value;
				}
			}
			return string.Empty;
		}

		public string KeywordEnum( int index )
		{
			if( m_createToggle )
			{
				return string.IsNullOrEmpty( PropertyName ) ? KeywordEnumList( index ) : ( PropertyName + "_" + KeywordEnumList( index ) );
			}
			else
			{
				return string.IsNullOrEmpty( PropertyName ) ? KeywordEnumList( index ) : ( PropertyName + KeywordEnumList( index ) );
			}
		}

		public string KeywordEnumList( int index )
		{
			if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
				return m_keywordEnumList[ index ];
			else
			{
				return m_createToggle ? m_keywordEnumList[ index ].ToUpper() : m_keywordEnumList[ index ];
			}

		}
		public override string PropertyName
		{
			get
			{
				if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
					return m_currentKeyword;
				else
				{
					return m_createToggle ? base.PropertyName.ToUpper() : base.PropertyName;
				}
			}
		}

		public override string GetPropertyValStr()
		{
			if ( m_keywordModeType == KeywordModeType.KeywordEnum )
				return PropertyName;
			else if( !m_lockKeyword )
				return CurrentKeyword;
			else if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
				return m_currentKeyword;
			else
				return PropertyName + OnOffStr;
		}

		public override string GetSubTitleVarNameFormatStr()
		{
			if ( m_multiCompile == ( int )KeywordType.DynamicBranch )
			{
				return "Dynamic( {0} )"; 
			}
			else
			{
				return "Static( {0} )";
			}
		}

		private string GetKeywordEnumPropertyList()
		{
			string result = string.Empty;
			for( int i = 0; i < m_keywordEnumList.Length; i++ )
			{
				if( i == 0 )
					result = m_keywordEnumList[ i ];
				else
					result += "," + m_keywordEnumList[ i ];
			}
			return result;
		}

		private string GetKeywordEnumPragmaList()
		{
			string result = string.Empty;
			for( int i = 0; i < m_keywordEnumList.Length; i++ )
			{
				if( i == 0 )
					result = KeywordEnum( i );
				else
					result += " " + KeywordEnum( i );
			}
			return result;
		}

		public override string GetUniformValue()
		{
			return string.Empty;
		}

		public override bool GetUniformData( out string dataType, out string dataName, ref bool fullValue )
		{
			dataType = string.Empty;
			dataName = string.Empty;
			return false;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );

			m_showErrorMessage = ( m_multiCompile == ( int )KeywordType.DynamicBranch && TemplateHelperFunctions.GetUnityVersion() < 20220100 );
		}

		public override void DrawProperties()
		{
			//base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, PropertyGroup );
			NodeUtils.DrawPropertyGroup( ref m_visibleCustomAttrFoldout, CustomAttrStr, DrawCustomAttributes, DrawCustomAttrAddRemoveButtons );
			CheckPropertyFromInspector();
		}

		void DrawEnumList()
		{
			EditorGUI.BeginChangeCheck();
			KeywordEnumAmount = EditorGUILayoutIntSlider( AmountStr, KeywordEnumAmount, 2, 9 );
			if( EditorGUI.EndChangeCheck() )
			{
				CurrentSelectedInput = Mathf.Clamp( CurrentSelectedInput, 0, KeywordEnumAmount - 1 );
				UpdateLabels();
			}
			EditorGUI.indentLevel++;
			for( int i = 0; i < m_keywordEnumList.Length; i++ )
			{
				EditorGUI.BeginChangeCheck();
				m_keywordEnumList[ i ] = EditorGUILayoutTextField( "Item " + i, m_keywordEnumList[ i ] );
				if( EditorGUI.EndChangeCheck() )
				{
					m_keywordEnumList[ i ] = UIUtils.RemoveInvalidEnumCharacters( m_keywordEnumList[ i ] );
					m_keywordEnumList[ i ] = m_keywordEnumList[ i ].Replace( " ", "" ); // sad face :( does not support spaces
					m_inputPorts[ i ].Name = m_keywordEnumList[ i ];
					m_defaultKeywordNames[ i ] = m_inputPorts[ i ].Name;
				}
			}
			EditorGUI.indentLevel--;
		}

		public void UpdateLabels()
		{
			int maxinputs = m_keywordModeType == KeywordModeType.KeywordEnum ? KeywordEnumAmount : 2;
			KeywordEnumAmount = Mathf.Clamp( KeywordEnumAmount, 0, maxinputs );
			m_keywordEnumList = new string[ maxinputs ];

			for( int i = 0; i < maxinputs; i++ )
			{
				m_keywordEnumList[ i ] = m_defaultKeywordNames[ i ];
				m_inputPorts[ i ].Name = m_keywordEnumList[ i ];
			}

			if( m_keywordModeType != KeywordModeType.KeywordEnum )
			{
				m_inputPorts[ 0 ].Name = "False";
				m_inputPorts[ 1 ].Name = "True";
			}

			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].Visible = ( i < maxinputs );
			}
			m_sizeIsDirty = true;
			m_isStaticSwitchDirty = true;
		}

		void PropertyGroup()
		{
			EditorGUI.BeginChangeCheck();
			var prevVarMode = CurrentVarMode;
			CurrentVarMode = (StaticSwitchVariableMode)EditorGUILayoutEnumPopup( ModeStr, CurrentVarMode );
			if( EditorGUI.EndChangeCheck() )
			{
				if ( prevVarMode == StaticSwitchVariableMode.Fetch && m_currentKeywordId == 0 )
				{
					m_propertyName = m_currentKeyword;
					m_propertyInspectorName = m_currentKeyword;
				}

				if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
				{
					m_keywordModeType = KeywordModeType.Toggle;
					UpdateLabels();
				}

				if( CurrentVarMode == StaticSwitchVariableMode.Reference )
				{
					UIUtils.UnregisterPropertyNode( this );
				}
				else
				{
					if( m_createToggle )
						UIUtils.RegisterPropertyNode( this );
					else
						UIUtils.UnregisterPropertyNode( this );
				}
			}

			if( CurrentVarMode == StaticSwitchVariableMode.Create )
			{
				EditorGUI.BeginChangeCheck();
				m_multiCompile = EditorGUILayoutIntPopup( KeywordTypeStr, m_multiCompile, KeywordTypeList, KeywordTypeInt );
				if( EditorGUI.EndChangeCheck() )
				{
					BeginPropertyFromInspectorCheck();
				}

				if ( m_showErrorMessage )
				{
					EditorGUILayout.HelpBox( DynamicBranchErrorMsg, MessageType.Error );
				}
			}
			else if( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				string[] arr = ContainerGraph.StaticSwitchNodes.NodesArr;
				bool guiEnabledBuffer = GUI.enabled;
				if( arr != null && arr.Length > 0 )
				{
					GUI.enabled = true;
				}
				else
				{
					m_referenceArrayId = -1;
					GUI.enabled = false;
				}

				EditorGUI.BeginChangeCheck();
				m_referenceArrayId = EditorGUILayoutPopup( Constants.AvailableReferenceStr, m_referenceArrayId, arr );
				if( EditorGUI.EndChangeCheck() )
				{
					m_reference = ContainerGraph.StaticSwitchNodes.GetNode( m_referenceArrayId );
					if( m_reference != null )
					{
						m_referenceNodeId = m_reference.UniqueId;
						CheckReferenceValues( true );
					}
					else
					{
						m_referenceArrayId = -1;
						m_referenceNodeId = -1;
					}
				}
				GUI.enabled = guiEnabledBuffer;

				return;
			}

			if( CurrentVarMode == StaticSwitchVariableMode.Create || m_createToggle )
			{
				EditorGUI.BeginChangeCheck();
				m_keywordModeType = (KeywordModeType)EditorGUILayoutEnumPopup( TypeStr, m_keywordModeType );
				if( EditorGUI.EndChangeCheck() )
				{
					UpdateLabels();
				}
			}

			if( m_keywordModeType != KeywordModeType.KeywordEnum )
			{
				if( CurrentVarMode == StaticSwitchVariableMode.Create || m_createToggle )
				{
					ShowPropertyInspectorNameGUI();
					ShowPropertyNameGUI( true );
					if( CurrentVarMode == StaticSwitchVariableMode.Create )
					{
						EditorGUILayout.BeginHorizontal();
						bool guiEnabledBuffer = GUI.enabled;
						GUI.enabled = !m_lockKeyword;
						if( m_lockKeyword )
							EditorGUILayout.TextField( KeywordNameStr, GetPropertyValStr() );
						else
							m_currentKeyword = EditorGUILayoutTextField( KeywordNameStr, m_currentKeyword );
						GUI.enabled = guiEnabledBuffer;
						m_lockKeyword = GUILayout.Toggle( m_lockKeyword, ( m_lockKeyword ? UIUtils.LockIconOpen : UIUtils.LockIconClosed ), "minibutton", GUILayout.Width( 22 ) );
						EditorGUILayout.EndHorizontal();
					}
				}
				
			}
			else
			{
				if( CurrentVarMode == StaticSwitchVariableMode.Create || m_createToggle )
				{
					ShowPropertyInspectorNameGUI();
					ShowPropertyNameGUI( true );
					DrawEnumList();
				}
				
			}

			if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
			{
				//ShowPropertyInspectorNameGUI();
				EditorGUI.BeginChangeCheck();
				m_currentKeywordId = EditorGUILayoutPopup( KeywordStr, m_currentKeywordId, UIUtils.AvailableKeywords );
				if( EditorGUI.EndChangeCheck() )
				{
					if( m_currentKeywordId != 0 )
					{
						m_currentKeyword = UIUtils.AvailableKeywords[ m_currentKeywordId ];
					}
				}

				if( m_currentKeywordId == 0 )
				{
					EditorGUI.BeginChangeCheck();
					m_currentKeyword = EditorGUILayoutTextField( CustomStr, m_currentKeyword );
					if( EditorGUI.EndChangeCheck() )
					{
						m_currentKeyword = UIUtils.RemoveInvalidCharacters( m_currentKeyword );
					}
				}
			}

			GUI.enabled = ( m_multiCompile != ( int )KeywordType.DynamicBranch );
			{
				m_isLocal = EditorGUILayoutToggle( IsLocalStr, m_isLocal );

				m_shaderStage = ( ShaderStage )EditorGUILayoutEnumPopup( StageStr, m_shaderStage );

				bool isFunction = ( m_containerGraph.CurrentCanvasMode == NodeAvailability.ShaderFunction );
				bool isTemplate = ( m_containerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader );
				if ( ( !isTemplate || isFunction ) && ( m_keywordModeType == KeywordModeType.KeywordEnum ) && ( m_shaderStage != ShaderStage.All ) )
				{
					EditorGUILayout.HelpBox( KeywordModifierSurfaceWarning, isFunction ? MessageType.Info : MessageType.Warning );
				}

				//if( CurrentVarMode == StaticSwitchVariableMode.Create )
				{
					ShowAutoRegister();
				}
			}
			GUI.enabled = true;

			EditorGUI.BeginChangeCheck();
			m_createToggle = EditorGUILayoutToggle( MaterialToggleStr, m_createToggle );
			if( EditorGUI.EndChangeCheck() )
			{
				if( m_createToggle )
					UIUtils.RegisterPropertyNode( this );
				else
					UIUtils.UnregisterPropertyNode( this );
			}
			
			
			if( m_createToggle )
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space( 20 );
				m_propertyTab = GUILayout.Toolbar( m_propertyTab, LabelToolbarTitle );
				EditorGUILayout.EndHorizontal();
				switch( m_propertyTab )
				{
					default:
					case 0:
					{
						EditorGUI.BeginChangeCheck();
						if( m_keywordModeType != KeywordModeType.KeywordEnum )
							m_materialValue = EditorGUILayoutToggle( ToggleMaterialValueStr, m_materialValue == 1 ) ? 1 : 0;
						else
							m_materialValue = EditorGUILayoutPopup( ToggleMaterialValueStr, m_materialValue, m_keywordEnumList );
						if( EditorGUI.EndChangeCheck() )
							m_requireMaterialUpdate = true;
					}
					break;
					case 1:
					{
						if( m_keywordModeType != KeywordModeType.KeywordEnum )
							m_defaultValue = EditorGUILayoutToggle( ToggleDefaultValueStr, m_defaultValue == 1 ) ? 1 : 0;
						else
							m_defaultValue = EditorGUILayoutPopup( ToggleDefaultValueStr, m_defaultValue, m_keywordEnumList );
					}
					break;
				}
			}

			//EditorGUILayout.HelpBox( "Keyword Type:\n" +
			//	"The difference is that unused variants of \"Shader Feature\" shaders will not be included into game build while \"Multi Compile\" variants are included regardless of their usage.\n\n" +
			//	"So \"Shader Feature\" makes most sense for keywords that will be set on the materials, while \"Multi Compile\" for keywords that will be set from code globally.\n\n" +
			//	"You can set keywords using the material property using the \"Property Name\" or you can set the keyword directly using the \"Keyword Name\".", MessageType.None );
		}

		public override void CheckPropertyFromInspector( bool forceUpdate = false )
		{
			if( m_propertyFromInspector )
			{
				if( forceUpdate || ( EditorApplication.timeSinceStartup - m_propertyFromInspectorTimestamp ) > MaxTimestamp )
				{
					m_propertyFromInspector = false;
					RegisterPropertyName( true, m_propertyInspectorName, m_autoGlobalName, m_underscoredGlobal );
					m_propertyNameIsDirty = true;

					if( CurrentVarMode != StaticSwitchVariableMode.Reference )
					{
						ContainerGraph.StaticSwitchNodes.UpdateDataOnNode( UniqueId, DataToArray );
					}
				}
			}
		}

		public override void OnNodeLayout( DrawInfo drawInfo )
		{
			float finalSize = 0;
			if( m_keywordModeType == KeywordModeType.KeywordEnum )
			{
				GUIContent dropdown = new GUIContent( m_inputPorts[ CurrentSelectedInput ].Name );
				int cacheSize = UIUtils.GraphDropDown.fontSize;
				UIUtils.GraphDropDown.fontSize = 10;
				Vector2 calcSize = UIUtils.GraphDropDown.CalcSize( dropdown );
				UIUtils.GraphDropDown.fontSize = cacheSize;
				finalSize = Mathf.Clamp( calcSize.x, MinComboSize, MaxComboSize );
				if( m_insideSize.x != finalSize )
				{
					m_insideSize.Set( finalSize, 25 );
					m_sizeIsDirty = true;
				}
			}

			base.OnNodeLayout( drawInfo );

			if( m_keywordModeType != KeywordModeType.KeywordEnum )
			{
				m_varRect = m_remainingBox;
				m_varRect.size = Vector2.one * 22 * drawInfo.InvertedZoom;
				m_varRect.center = m_remainingBox.center;
				if( m_showPreview )
					m_varRect.y = m_remainingBox.y;
			}
			else
			{
				m_varRect = m_remainingBox;
				m_varRect.width = finalSize * drawInfo.InvertedZoom;
				m_varRect.height = 16 * drawInfo.InvertedZoom;
				m_varRect.x = m_remainingBox.xMax - m_varRect.width;
				m_varRect.y += 1 * drawInfo.InvertedZoom;

				m_imgRect = m_varRect;
				m_imgRect.x = m_varRect.xMax - 16 * drawInfo.InvertedZoom;
				m_imgRect.width = 16 * drawInfo.InvertedZoom;
				m_imgRect.height = m_imgRect.width;
			}

			CheckReferenceValues( false );

			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
			{
				m_iconPos = m_globalPosition;
				m_iconPos.width = InstanceIconWidth * drawInfo.InvertedZoom;
				m_iconPos.height = InstanceIconHeight * drawInfo.InvertedZoom;

				m_iconPos.y += 10 * drawInfo.InvertedZoom;
				m_iconPos.x += /*m_globalPosition.width - m_iconPos.width - */5 * drawInfo.InvertedZoom;
			}

		}

		void CheckReferenceValues( bool forceUpdate )
		{
			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
			{
				if( m_reference == null && m_referenceNodeId > 0 )
				{
					m_reference = ContainerGraph.GetNode( m_referenceNodeId ) as StaticSwitch;
					m_referenceArrayId = ContainerGraph.StaticSwitchNodes.GetNodeRegisterIdx( m_referenceNodeId );
				}

				if( m_reference != null )
				{
					if( forceUpdate || m_reference.IsStaticSwitchDirty )
					{
						int count = m_inputPorts.Count;
						for( int i = 0; i < count; i++ )
						{
							m_inputPorts[ i ].Name = m_reference.InputPorts[ i ].Name;
							m_inputPorts[ i ].Visible = m_reference.InputPorts[ i ].Visible;
						}
						m_sizeIsDirty = true;
					}
				}
			}
			else
			{
				m_isStaticSwitchDirty = false;
			}
		}

		public override void DrawGUIControls( DrawInfo drawInfo )
		{
			base.DrawGUIControls( drawInfo );

			if( drawInfo.CurrentEventType != EventType.MouseDown || !m_createToggle )
				return;

			if( m_varRect.Contains( drawInfo.MousePosition ) )
			{
				m_editing = true;
			}
			else if( m_editing )
			{
				m_editing = false;
			}
		}

		private int CurrentSelectedInput
		{
			get
			{
				return m_materialMode ? m_materialValue : m_defaultValue;
			}
			set
			{
				if( m_materialMode )
					m_materialValue = value;
				else
					m_defaultValue = value;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
				return;

			if( m_editing )
			{
				if( m_keywordModeType != KeywordModeType.KeywordEnum )
				{
					if( GUI.Button( m_varRect, GUIContent.none, UIUtils.GraphButton ) )
					{
						CurrentSelectedInput = CurrentSelectedInput == 1 ? 0 : 1;
						PreviewIsDirty = true;
						m_editing = false;
						if( m_materialMode )
							m_requireMaterialUpdate = true;
					}

					if( CurrentSelectedInput == 1 )
					{
						GUI.Label( m_varRect, m_checkContent, UIUtils.GraphButtonIcon );
					}
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					CurrentSelectedInput = EditorGUIPopup( m_varRect, CurrentSelectedInput, m_keywordEnumList, UIUtils.GraphDropDown );
					if( EditorGUI.EndChangeCheck() )
					{
						PreviewIsDirty = true;
						m_editing = false;
						if( m_materialMode )
							m_requireMaterialUpdate = true;
					}
				}
			}
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );

			if( !m_isVisible )
				return;

			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
			{
				GUI.Label( m_iconPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon ) );
				return;
			}

			if( m_createToggle && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD2 )
			{
				if( !m_editing )
				{
					if( m_keywordModeType != KeywordModeType.KeywordEnum )
					{
						GUI.Label( m_varRect, GUIContent.none, UIUtils.GraphButton );

						if( CurrentSelectedInput == 1 )
							GUI.Label( m_varRect, m_checkContent, UIUtils.GraphButtonIcon );
					}
					else
					{
						GUI.Label( m_varRect, m_keywordEnumList[ CurrentSelectedInput ], UIUtils.GraphDropDown );
						GUI.Label( m_imgRect, m_popContent, UIUtils.GraphButtonIcon );
					}
				}
			}
		}

		private string OnOffStr
		{
			get
			{
				if( !m_lockKeyword )
					return string.Empty;

				StaticSwitch node = null;
				switch( CurrentVarMode )
				{
					default:
					case StaticSwitchVariableMode.Create:
					case StaticSwitchVariableMode.Fetch:
					node = this;
					break;
					case StaticSwitchVariableMode.Reference:
					{
						node = ( m_reference != null ) ? m_reference : this;
					}
					break;
				}

				if( !node.CreateToggle )
					return string.Empty;

				switch( node.KeywordModeTypeValue )
				{
					default:
					case KeywordModeType.Toggle:
					return "_ON";
					case KeywordModeType.ToggleOff:
					return "_OFF";
				}
			}
		}
		string GetStaticSwitchType( bool allowStages )
		{
			string staticSwitchType;
			bool allowModifiers = true;

			switch ( m_multiCompile )
			{
				case 1:
					staticSwitchType = "multi_compile";
					break;
				case 2:
					staticSwitchType = "dynamic_branch";
					allowModifiers = false;
					break;
				default:
					staticSwitchType = "shader_feature";
					break;
			}

			if ( allowModifiers )
			{
				if ( m_isLocal )
				{
					staticSwitchType += "_local";
				}

				if ( allowStages )
				{
					switch ( m_shaderStage )
					{
						default:
						case ShaderStage.All: break;
						case ShaderStage.Vertex: staticSwitchType += "_vertex"; break;
						case ShaderStage.Fragment: staticSwitchType += "_fragment"; break;
						case ShaderStage.Hull: staticSwitchType += "_hull"; break;
						case ShaderStage.Domain: staticSwitchType += "_domain"; break;
						case ShaderStage.Geometry: staticSwitchType += "_geometry"; break;
						case ShaderStage.Raytracing: staticSwitchType += "_raytracing"; break;
					}
				}
			}
			return staticSwitchType;
		}

		void RegisterPragmas( ref MasterNodeDataCollector dataCollector )
		{
			if( CurrentVarMode == StaticSwitchVariableMode.Create )
			{
				string staticSwitchType = GetStaticSwitchType( allowStages: dataCollector.IsTemplate || ( m_keywordModeType != KeywordModeType.KeywordEnum ) );

				if( m_keywordModeType == KeywordModeType.KeywordEnum )
				{
					dataCollector.AddToPragmas( UniqueId, staticSwitchType + " " + GetKeywordEnumPragmaList() );
				}
				else
				{
					if( m_multiCompile == 1 )
						dataCollector.AddToPragmas( UniqueId, staticSwitchType + " __ " + CurrentKeyword );
					else
						dataCollector.AddToPragmas( UniqueId, staticSwitchType + " " + CurrentKeyword );
				}
			}
		}

		protected override void RegisterProperty( ref MasterNodeDataCollector dataCollector )
		{
			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null )
			{
				m_reference.RegisterProperty( ref dataCollector );
				m_reference.RegisterPragmas( ref dataCollector );
			}
			else
			{
				if( m_createToggle )
					base.RegisterProperty( ref dataCollector );

				RegisterPragmas( ref dataCollector );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			if ( m_showErrorMessage )
			{
				UIUtils.ShowMessage( DynamicBranchErrorMsg );
				return GenerateErrorValue();
			}

			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			StaticSwitch node = ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

			this.OrderIndex = node.RawOrderIndex;
			this.OrderIndexOffset = node.OrderIndexOffset;

			string outType = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType );

			if( node.KeywordModeTypeValue == KeywordModeType.KeywordEnum )
			{
				string[] allOutputs = new string[ node.KeywordEnumAmount ];
				for ( int i = 0; i < node.KeywordEnumAmount; i++ )
					allOutputs[ i ] = m_inputPorts[ i ].GeneratePortInstructions( ref dataCollector );

				if ( m_multiCompile == ( int )KeywordType.DynamicBranch )
				{
					dataCollector.AddLocalVariable( UniqueId, outType + " dynamicSwitch" + OutputId + " = ( " + outType + " )0;", true );
					for ( int i = 0; i < node.KeywordEnumAmount; i++ )
					{
						string keyword = node.KeywordEnum( i );
						if ( i == 0 )
							dataCollector.AddLocalVariable( UniqueId, "UNITY_BRANCH if ( " + keyword + " )", true );
						else
							dataCollector.AddLocalVariable( UniqueId, "else if ( " + keyword + " )", true );

						dataCollector.AddLocalVariable( UniqueId, "{", true );

						dataCollector.AddLocalVariable( UniqueId, "\tdynamicSwitch" + OutputId + " = " + allOutputs[ i ] + ";", true );

						dataCollector.AddLocalVariable( UniqueId, "}", true );
					}
					dataCollector.AddLocalVariable( UniqueId, "else", true );
					dataCollector.AddLocalVariable( UniqueId, "{", true );
					dataCollector.AddLocalVariable( UniqueId, "\tdynamicSwitch" + OutputId + " = " + allOutputs[ node.DefaultValue ] + ";", true );
					dataCollector.AddLocalVariable( UniqueId, "}", true );
				}
				else
				{
					for ( int i = 0; i < node.KeywordEnumAmount; i++ )
					{
						string keyword = node.KeywordEnum( i );
						if ( i == 0 )
							dataCollector.AddLocalVariable( UniqueId, "#if defined( " + keyword + " )", true );
						else
							dataCollector.AddLocalVariable( UniqueId, "#elif defined( " + keyword + " )", true );

						dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " staticSwitch" + OutputId + " = " + allOutputs[ i ] + ";", true );
					}
					dataCollector.AddLocalVariable( UniqueId, "#else", true );
					dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " staticSwitch" + OutputId + " = " + allOutputs[ node.DefaultValue ] + ";", true );
					dataCollector.AddLocalVariable( UniqueId, "#endif", true );
				}
			}
			else
			{
				string falseCode = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
				string trueCode = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

				if ( m_multiCompile == ( int )KeywordType.DynamicBranch )
				{
					dataCollector.AddLocalVariable( UniqueId, outType + " dynamicSwitch" + OutputId + " = ( " + outType + " )0;", true );
					dataCollector.AddLocalVariable( UniqueId, "UNITY_BRANCH if ( " + node.CurrentKeyword + " )", true );
					dataCollector.AddLocalVariable( UniqueId, "{", true );
					dataCollector.AddLocalVariable( UniqueId, "\tdynamicSwitch" + OutputId + " = " + trueCode + ";", true );
					dataCollector.AddLocalVariable( UniqueId, "}", true );
					dataCollector.AddLocalVariable( UniqueId, "else", true );
					dataCollector.AddLocalVariable( UniqueId, "{", true );
					dataCollector.AddLocalVariable( UniqueId, "\tdynamicSwitch" + OutputId + " = " + falseCode + ";", true );
					dataCollector.AddLocalVariable( UniqueId, "}", true );
					m_outputPorts[ 0 ].SetLocalValue( "dynamicSwitch" + OutputId, dataCollector.PortCategory );
				}
				else
				{
					//if( node.CurrentVarMode == StaticSwitchVariableMode.Fetch )
					dataCollector.AddLocalVariable( UniqueId, "#ifdef " + node.CurrentKeyword, true );
					//else
					//	dataCollector.AddLocalVariable( UniqueId, "#ifdef " + node.PropertyName + OnOffStr, true );
					dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " staticSwitch" + OutputId + " = " + trueCode + ";", true );
					dataCollector.AddLocalVariable( UniqueId, "#else", true );
					dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " staticSwitch" + OutputId + " = " + falseCode + ";", true );
					dataCollector.AddLocalVariable( UniqueId, "#endif", true );
					m_outputPorts[ 0 ].SetLocalValue( "staticSwitch" + OutputId, dataCollector.PortCategory );
				}
			}

			if ( m_multiCompile == ( int )KeywordType.DynamicBranch )
			{
				m_outputPorts[ 0 ].SetLocalValue( "dynamicSwitch" + OutputId, dataCollector.PortCategory );
			}
			else
			{
				m_outputPorts[ 0 ].SetLocalValue( "staticSwitch" + OutputId, dataCollector.PortCategory );
			}
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void DrawTitle( Rect titlePos )
		{
			bool referenceMode = m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null;
			string subTitle = string.Empty;
			string subTitleFormat = string.Empty;
			if( referenceMode )
			{
				subTitle = m_reference.GetPropertyValStr();
				subTitleFormat = Constants.SubTitleRefNameFormatStr;
			}
			else
			{
				subTitle = GetPropertyValStr();
				subTitleFormat = GetSubTitleVarNameFormatStr();
			}

			SetAdditonalTitleTextOnCallback( subTitle, subTitleFormat, ( instance, newSubTitle ) => instance.AdditonalTitleContent.text = string.Format( subTitleFormat, newSubTitle ) );

			if( !m_isEditing && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				GUI.Label( titlePos, StaticSwitchStr, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
			}
		}

		public override void UpdateMaterial( Material mat )
		{
			base.UpdateMaterial( mat );
			if( UIUtils.IsProperty( m_currentParameterType ) && !InsideShaderFunction )
			{
				if( m_keywordModeType == KeywordModeType.KeywordEnum )
				{
					for( int i = 0; i < m_keywordEnumAmount; i++ )
					{
						string key = KeywordEnum( i );
						mat.DisableKeyword( key );
					}
					mat.EnableKeyword( KeywordEnum( m_materialValue ));
					mat.SetFloat( m_propertyName, m_materialValue );
				}
				else
				{
					int final = m_materialValue;
					if( m_keywordModeType == KeywordModeType.ToggleOff )
						final = final == 1 ? 0 : 1;
					mat.SetFloat( m_propertyName, m_materialValue );
					if( final == 1 )
						mat.EnableKeyword( GetPropertyValStr() );
					else
						mat.DisableKeyword( GetPropertyValStr() );
				}
			}
		}

		public override void SetMaterialMode( Material mat, bool fetchMaterialValues )
		{
			base.SetMaterialMode( mat, fetchMaterialValues );
			if( fetchMaterialValues && m_materialMode && UIUtils.IsProperty( m_currentParameterType ) && mat.HasProperty( m_propertyName ) )
			{
				m_materialValue = mat.GetInt( m_propertyName );
			}
		}

		public override void ForceUpdateFromMaterial( Material material )
		{
			if( UIUtils.IsProperty( m_currentParameterType ) && material.HasProperty( m_propertyName ) )
			{
				m_materialValue = material.GetInt( m_propertyName );
				PreviewIsDirty = true;
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_multiCompile = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 14403 )
			{
				m_defaultValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				if( UIUtils.CurrentShaderVersion() > 14101 )
				{
					m_materialValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
			}
			else
			{
				m_defaultValue = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) ) ? 1 : 0;
				if( UIUtils.CurrentShaderVersion() > 14101 )
				{
					m_materialValue = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) ) ? 1 : 0;
				}
			}

			if( UIUtils.CurrentShaderVersion() > 13104 )
			{
				m_createToggle = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_currentKeyword = GetCurrentParam( ref nodeParams );
				m_currentKeywordId = UIUtils.GetKeywordId( m_currentKeyword );
			}
			if( UIUtils.CurrentShaderVersion() > 14001 )
			{
				m_keywordModeType = (KeywordModeType)Enum.Parse( typeof( KeywordModeType ), GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 14403 )
			{
				KeywordEnumAmount = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				for( int i = 0; i < KeywordEnumAmount; i++ )
				{
					m_defaultKeywordNames[ i ] = GetCurrentParam( ref nodeParams );
				}

				UpdateLabels();
			}

			if( UIUtils.CurrentShaderVersion() > 16304 )
			{
				string currentVarMode = GetCurrentParam( ref nodeParams );
				CurrentVarMode = (StaticSwitchVariableMode)Enum.Parse( typeof( StaticSwitchVariableMode ), currentVarMode );
				if( CurrentVarMode == StaticSwitchVariableMode.Reference )
				{
					m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
			}
			else
			{
				CurrentVarMode = (StaticSwitchVariableMode)m_variableMode;
				//Resetting m_variableMode to its default value since it will no longer be used and interfere released ransom properties behavior
				m_variableMode = VariableMode.Create;
			}

			if( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				UIUtils.UnregisterPropertyNode( this );
			}
			else
			{
				if( m_createToggle )
					UIUtils.RegisterPropertyNode( this );
				else
					UIUtils.UnregisterPropertyNode( this );
			}

			if( UIUtils.CurrentShaderVersion() > 16700 )
			{
				m_isLocal = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 18401 )
				m_lockKeyword = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );

			if( UIUtils.CurrentShaderVersion() > 18928 )
				m_shaderStage = (ShaderStage)Enum.Parse( typeof(ShaderStage), GetCurrentParam( ref nodeParams ) );


			SetMaterialToggleRetrocompatibility();

			if( !m_isNodeBeingCopied && CurrentVarMode != StaticSwitchVariableMode.Reference )
			{
				ContainerGraph.StaticSwitchNodes.UpdateDataOnNode( UniqueId, DataToArray );
			}
		}

		public override void ReleaseRansomedProperty()
		{
			//on old ASE, the property node m_variableMode was used on defining the static switch type, now we have a specific m_staticSwitchVarMode over here
			//the problem with this is the fix made to release ransomend property names( hash deb232819fff0f1aeaf029a21c55ef597b3424de ) uses m_variableMode and 
			//makes old static switches to attempt and register an already registered name when doing this:
			//CurrentVariableMode = VariableMode.Create;
			//So we need to disable this release ransom property behavior as m_variableMode should never be on VariableMode.Create 
			//The m_variableMode is set to its default value over the ReadFromString method after its value as been set over the new m_staticSwitchVarMode variable
		}

		void SetMaterialToggleRetrocompatibility()
		{
			if( UIUtils.CurrentShaderVersion() < 17108 )
			{
				if( !m_createToggle && m_staticSwitchVarMode == StaticSwitchVariableMode.Create )
				{
					if( m_keywordModeType != KeywordModeType.KeywordEnum )
					{
						m_propertyName = m_propertyName.ToUpper() + "_ON";
					}
					else
					{
						m_propertyName = m_propertyName.ToUpper();
						for( int i = 0; i < m_keywordEnumList.Length; i++ )
						{
							m_keywordEnumList[ i ] = "_" + m_keywordEnumList[ i ].ToUpper();
						}
					}
					m_autoGlobalName = false;
				}
			}
		}

		public override void ReadFromDeprecated( ref string[] nodeParams, Type oldType = null )
		{
			base.ReadFromDeprecated( ref nodeParams, oldType );
			{
				m_currentKeyword = GetCurrentParam( ref nodeParams );
				m_currentKeywordId = UIUtils.GetKeywordId( m_currentKeyword );
				m_createToggle = false;
				m_keywordModeType = KeywordModeType.Toggle;
				m_variableMode = VariableMode.Fetch;
				CurrentVarMode = StaticSwitchVariableMode.Fetch;
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_multiCompile );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_materialValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_createToggle );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentKeyword );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_keywordModeType );
			IOUtils.AddFieldValueToString( ref nodeInfo, KeywordEnumAmount );
			for( int i = 0; i < KeywordEnumAmount; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_keywordEnumList[ i ] );
			}

			IOUtils.AddFieldValueToString( ref nodeInfo, CurrentVarMode );
			if( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				int referenceId = ( m_reference != null ) ? m_reference.UniqueId : -1;
				IOUtils.AddFieldValueToString( ref nodeInfo, referenceId );
			}
			IOUtils.AddFieldValueToString( ref nodeInfo, m_isLocal );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_lockKeyword );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_shaderStage );
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			CheckReferenceValues( true );
		}

		StaticSwitchVariableMode CurrentVarMode
		{
			get { return m_staticSwitchVarMode; }
			set
			{
				if( m_staticSwitchVarMode != value )
				{
					if( value == StaticSwitchVariableMode.Reference )
					{
						ContainerGraph.StaticSwitchNodes.RemoveNode( this );
						m_referenceArrayId = -1;
						m_referenceNodeId = -1;
						m_reference = null;
						m_headerColorModifier = ReferenceHeaderColor;
					}
					else
					{
						m_headerColorModifier = Color.white;
						ContainerGraph.StaticSwitchNodes.AddNode( this );
						UpdateLabels();
					}
				}
				m_staticSwitchVarMode = value;
			}
		}
		public bool IsStaticSwitchDirty { get { return m_isStaticSwitchDirty; } }
		public KeywordModeType KeywordModeTypeValue { get { return m_keywordModeType; } }
		public int DefaultValue { get { return m_defaultValue; } }
		public int MaterialValue { get { return m_materialValue; } }
		//public string CurrentKeyword { get { return m_currentKeyword; } }
		public string CurrentKeyword
		{
			get
			{
				if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
					return m_currentKeyword;

				return ( m_lockKeyword || string.IsNullOrEmpty( m_currentKeyword ) ? PropertyName + OnOffStr : m_currentKeyword );
			}
		}
		public bool CreateToggle { get { return m_createToggle; } }

		public int KeywordEnumAmount
		{
			get
			{
				return m_keywordEnumAmount;
			}
			set
			{
				m_keywordEnumAmount = value;
				m_defaultValue = Mathf.Clamp( m_defaultValue, 0, m_keywordEnumAmount - 1 );
				m_materialValue = Mathf.Clamp( m_defaultValue, 0, m_keywordEnumAmount - 1 );
			}
		}
	}
}
