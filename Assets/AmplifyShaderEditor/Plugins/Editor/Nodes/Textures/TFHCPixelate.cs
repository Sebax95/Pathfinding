// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//
// Custom Node Pixelate UV
// Donated by The Four Headed Cat - @fourheadedcat

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Pixelate UV", "UV Coordinates", "Pixelate Texture Modifying UV.", null, KeyCode.None, true, false, null, null, "The Four Headed Cat - @fourheadedcat" )]
	public sealed class TFHCPixelate : ParentNode
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT2, true, "UV" );
			AddInputPort( WirePortDataType.FLOAT, false, "Pixels X" );
			AddInputPort( WirePortDataType.FLOAT, false, "Pixels Y" );
			AddInputPort( WirePortDataType.FLOAT2, false, "Pixel Offset" );
			AddOutputPort( WirePortDataType.FLOAT2, "Out" );
			m_useInternalPortData = true;
			m_previewShaderGUID = "e2f7e3c513ed18340868b8cbd0d85cfb";
		}

		public override void DrawProperties()
		{
			base.DrawProperties ();
			EditorGUILayout.HelpBox (
				"Pixelate UV.\n\n" +
				"  - UV is the Texture Coordinates to pixelate.\n" +
				"  - Pixels X is the number of horizontal pixels.\n" +
				"  - Pixels Y is the number of vertical pixels.\n" +
				"  - Pixel Offset is relative to pixel corner; e.g. pixel center is at offset (0.5,0.5).",
				MessageType.None );

		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			string uv = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string PixelCount_X = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			string PixelCount_Y = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
			string PixelOffset = m_inputPorts[ 3 ].GeneratePortInstructions( ref dataCollector );

			string pixelatedUV = string.Format( "half2 pixelateduv{0} = floor( {1} * float2( {2}, {3} ) + {4} ) / float2( {2}, {3} );",
				OutputId, uv, PixelCount_X, PixelCount_Y, PixelOffset );

			string result = "pixelateduv" + OutputId;

			dataCollector.AddLocalVariable( UniqueId, pixelatedUV );

			return GetOutputVectorItem( 0, outputId, result);

		}
	}
}
