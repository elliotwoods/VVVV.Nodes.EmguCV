//@author: elliot woods
//@help: looks backwards using optical flow to find old data
//@tags: template, basic
//@credits:

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

//transforms
float4x4 tW: WORLD;        //the models world matrix
float4x4 tV: VIEW;         //view matrix as set via Renderer (EX9)
float4x4 tP: PROJECTION;   //projection matrix as set via Renderer (EX9)
float4x4 tWVP: WORLDVIEWPROJECTION;

//texture
texture TexP <string uiname="Previous frame";>;
sampler SampP = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (TexP);          //apply a texture to the sampler
    MipFilter = LINEAR;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

//texture
texture TexC <string uiname="Current frame";>;
sampler SampC = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (TexC);          //apply a texture to the sampler
    MipFilter = LINEAR;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

//texture
texture TexF <string uiname="Optical flow";>;
sampler SampF = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (TexF);          //apply a texture to the sampler
    MipFilter = LINEAR;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

int ImageWidth=640;
int ImageHeight=480;

//the data structure: vertexshader to pixelshader
//used as output data with the VS function
//and as input data with the PS function
struct vs2ps
{
    float4 Pos : POSITION;
    float4 TexCd : TEXCOORD0;
};

// --------------------------------------------------------------------------------------------------
// VERTEXSHADERS
// --------------------------------------------------------------------------------------------------

vs2ps VS(
    float4 Pos : POSITION,
    float4 TexCd : TEXCOORD0)
{
    //inititalize all fields of output struct with 0
    vs2ps Out = (vs2ps)0;

	Pos.xy *=2;
    //transform position
    Out.Pos = mul(Pos, tWVP);

    //transform texturecoordinates
    Out.TexCd = TexCd;

    return Out;
}

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

float4 PS(vs2ps In): COLOR
{
    //In.TexCd = In.TexCd / In.TexCd.w; // for perpective texture projections (e.g. shadow maps) ps_2_0
	
	float2 DFactor = 1/float2(ImageWidth,ImageHeight);
	float2 flow = tex2D(SampF, In.TexCd).xy;
    float4 col = tex2D(SampP, In.TexCd - flow * DFactor);
    return col;
}

float dt = 1/30;

float threshold = 2;
float4 PSDerivative(vs2ps In) : COLOR
{
    //In.TexCd = In.TexCd / In.TexCd.w; // for perpective texture projections (e.g. shadow maps) ps_2_0
	
	float2 DFactor = 1/float2(ImageWidth,ImageHeight);
	float2 flow = tex2D(SampF, In.TexCd).xy;
	
	float4 p = tex2D(SampP, In.TexCd - flow * DFactor);
	float4 c = tex2D(SampC, In.TexCd);
    float4 v = (c-p) / dt;
	v.a = 1;
	v.a *= p.xyz.z > 0.5 * c.xyz.z > 0.5;
    return v;
}

// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique TLookup
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}

technique TDerivative
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PSDerivative();
    }
}