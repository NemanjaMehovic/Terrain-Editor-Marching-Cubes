Shader"Custom/ColorShader"
{
    Properties 
    {
	    _AmbientStrength ("Ambient" , Float) = 0.5
	    _SpecularStrength ("Specular", Float) = 0.5
        _Color ("Color", Color) = (1, 0, 0, 1)
		_SecondaryColor ("Secondary Color", Color) = (0, 1, 0, 1)
        _ThirdColor ("Third Color", Color) = (1, 0, 1, 1)
        _MaxHeight ("Max Height", Float) = 20.0
    }
    SubShader 
    {
        Cull Off
        Tags 
        {
            "LightMode" = "ForwardBase"
        }
        LOD 100
        Pass 
        {
            HLSLPROGRAM
            #pragma vertex vertexShader
            #pragma fragment fragmentShader

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct vertexToFragment
            {
                float3 normal : NORMAL;
                float4 vertex : SV_POSITION;
                float3 lightDirection : TEXCOORD1;
                float3 viewDirection : TEXCOORD2;
                float4 color : COLOR;
            };

            float _AmbientStrength;
            float _SpecularStrength;

            fixed4 _Color;
            fixed4 _SecondaryColor;
            fixed4 _ThirdColor;
            fixed _MaxHeight;

            vertexToFragment vertexShader(appdata data)
            {
                vertexToFragment output;
                
                output.vertex = UnityObjectToClipPos(data.vertex);
                output.normal = UnityObjectToWorldNormal(data.normal);

                float3 worldPosition = mul(unity_ObjectToWorld, data.vertex).xyz;
                
                output.lightDirection = normalize(UnityWorldSpaceLightDir(worldPosition));
                output.viewDirection = normalize(_WorldSpaceCameraPos.xyz - worldPosition.xyz);
    
                float blend;
                if (worldPosition.y < (_MaxHeight / 2))
                {
                    float secondColorMax = (_MaxHeight / 2);
                    blend = worldPosition.y / (_MaxHeight / 2);
                    output.color = lerp(_Color, _SecondaryColor, blend);
                }
                else
                {
                    blend = (worldPosition.y - (_MaxHeight / 2)) / (_MaxHeight / 2);
                    output.color = lerp(_SecondaryColor, _ThirdColor, blend);
                }
                
                return output;
            }

            fixed4 fragmentShader(vertexToFragment data) : SV_Target
            {
                fixed4 color = data.color;
                float3 ambient = _AmbientStrength * _LightColor0;

                float3 normal = normalize(data.normal);
                float3 lightDirection = normalize(data.lightDirection);
                float diff = max(0, dot(normal, lightDirection));
                float3 diffuse = diff * _LightColor0;

                float3 reflection = reflect(-lightDirection, normal);
                float spec = pow(max(0, dot(normalize(data.viewDirection), reflection)), 25);
                float3 specular = _SpecularStrength * spec * _LightColor0;
                float3 temp = ambient + diffuse + specular;
                fixed4 res = fixed4(fixed3(temp.x, temp.y, temp.z), 1.0) * color;
                return res;
            }
            ENDHLSL
        }
    }
}
