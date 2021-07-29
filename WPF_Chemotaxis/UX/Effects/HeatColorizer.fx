﻿/// <summary>The implicit input sampler passed into the pixel shader by WPF.</summary>
sampler2D Input : register(s0);

/// <summary>The 1x256 color map gradient to use to colorize the incoming grayscale input sampler.</summary>
sampler2D Palette : register(s1);

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 c = tex2D(Input, uv.xy);
	float2 index = float2(c.x, 0);
	float4 heat = tex2D(Palette, index);
	return heat;
}