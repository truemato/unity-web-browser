Shader "Custom/TransparentMask"
{
    SubShader
    {
        Tags { "Queue" = "Geometry-1" }
        ColorMask 0
        ZWrite On
        Blend Zero One

        Pass {}
    }
}
