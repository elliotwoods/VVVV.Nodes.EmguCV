//@author: vvvv group
//@help: this is a very basic template. use it to start writing your own effects. if you want effects with lighting start from one of the GouraudXXXX or PhongXXXX effects
//@tags:
//@credits:

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

//transforms
float4x4 tW: WORLD;        //the models world matrix
float4x4 tV: VIEW;         //view matrix as set via Renderer (EX9)
float4x4 tP: PROJECTION;
float4x4 tWVP: WORLDVIEWPROJECTION;

float4x4 tScreen;

//texture
texture Tex <string uiname="XYZ";>;
sampler Samp = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (Tex);          //apply a texture to the sampler
    MipFilter = POINT;         //sampler states
    MinFilter = POINT;
    MagFilter = NONE;
};

texture TexRGB <string uiname="RGB";>;
sampler SampRGB = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (TexRGB);          //apply a texture to the sampler
    MipFilter = LINEAR;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

texture TexNorm <string uiname="Normals";>;
sampler SampNorm = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (TexNorm);          //apply a texture to the sampler
    MipFilter = LINEAR;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

float deadzone = 0.2;

float maxjump=0.2;
float drop = 0.9;
//texture transformation marked with semantic TEXTUREMATRIX to achieve symmetric transformations
float4x4 tTex: TEXTUREMATRIX <string uiname="Texture Transform";>;

//the data structure: "vertexshader to pixelshader"
//used as output data with the VS function
//and as input data with the PS function
struct vs2ps
{
    float4 Pos  : POSITION;
    float2 TexCd : TEXCOORD0;
	float existence : TEXCOORD1;
	float3 PosW : TEXCOORD2;
};

// --------------------------------------------------------------------------------------------------
// VERTEXSHADERS
// --------------------------------------------------------------------------------------------------
bool jumps (float2 TexCd)
{
	float o  = false;
	float2 dv = 1.0f / float2(640.0f, 480.0f);
	int steps = 1;
	float r = length(tex2Dlod(Samp, float4(TexCd.x, TexCd.y, 0, 0)).xyz);
	float r2;
	for (float x = -dv.x * steps + TexCd.x; x <= dv.x * steps + TexCd.x; x+= dv.x)
	{
		for (float y = -dv.y * steps + TexCd.y; y <= dv.y * steps + TexCd.y; y+= dv.y)
		{
			r2 = length(tex2Dlod(Samp, float4(x, y, 0, 0)).xyz);
			if (r - r2 > maxjump || r2 <= deadzone)
				o = true;
		}
	}
	
	return o;
}
vs2ps VS(
    float4 TexCd : TEXCOORD0)
{
    //declare output struct
    vs2ps Out;

    //transform texturecoordinates
    Out.TexCd = mul(TexCd, tTex);
	
	float4 TexCdl = float4(TexCd.x, TexCd.y, 0, 0);
	float4 PosO = tex2Dlod(Samp, TexCdl);
	
	float4 p = mul(PosO,tWVP);
	bool zero = jumps(TexCd);
	
	p.w *= !zero;
	p.z = zero ? 5 : p.z;
	
	float4 s = mul(p, tScreen);
	Out.Pos = s;
	
	
	Out.existence = !zero;

	
	Out.PosW = Out.Pos.xyz;
    return Out;
}

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

float4 PS(vs2ps In): COLOR
{
    float4 col = tex2D(SampRGB, In.TexCd);
	col.a = In.existence > drop;
    return col;
}

float4 PSNormals(vs2ps In): COLOR
{
    float4 col = tex2D(SampNorm, In.TexCd);
	col.a = In.existence > drop;
    return col;
}

float3 LightDirection;
float4 PSLight(vs2ps In): COLOR
{
    float4 col = dot(LightDirection, tex2D(SampNorm, In.TexCd));
	col.a = In.existence > drop;
    return col;
}

float3 LightPosition;
float MaxRange = 2.0f;
float MaxDepth = 1.5f;
float Mult  = 1.0f;
float4 PSLightPoint(vs2ps In): COLOR
{
	float3 lv = In.PosW - LightPosition;
    float4 col = dot(lv, tex2D(SampNorm, In.TexCd));
	float l = length(lv);
	col /= pow(l, 2);
	col*= Mult;
	if (l > MaxRange || In.PosW.z > MaxDepth)
		col = 0;
	col.a = In.existence > drop;
    return col;
}


// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique TPreview
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PS();
    }
}


technique TNormals
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PSNormals();
    }
}

technique TLightDirectional
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PSLight();
    }
}

technique TLightPoint
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PSLightPoint();
    }
}