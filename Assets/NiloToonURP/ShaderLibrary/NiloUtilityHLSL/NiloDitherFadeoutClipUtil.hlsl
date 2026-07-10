// SPDX-License-Identifier: (Not available for this version, you are only allowed to use this software if you have express permission from the copyright holder and agreed to the latest NiloToonURP EULA)
// Copyright (c) 2021 Kuroneko ShaderLab Limited

// For more information, visit -> https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

// #pragma once is a safe guard best practice in almost every .hlsl, 
// doing this can make sure your .hlsl's user can include this .hlsl anywhere anytime without producing any multi include conflict
#pragma once

void NiloDoDitherFadeoutClip(float2 SV_POSITIONxy, float ditherOpacity)
{
    // copy from https://docs.unity3d.com/Packages/com.unity.shadergraph@10.3/manual/Dither-Node.html?q=dither
    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(SV_POSITIONxy.x) % 4) * 4 + uint(SV_POSITIONxy.y) % 4;
    clip(ditherOpacity - DITHER_THRESHOLDS[index]);
}
// this will work for iPhone15Pro, but the original function wont, why? must be due to % or array.
/*
void NiloDoDitherFadeoutClip(float2 SV_POSITIONxy, float ditherOpacity)
{
    // Use integer coordinates
    int x = int(SV_POSITIONxy.x) & 3; // Using bitwise AND instead of modulo
    int y = int(SV_POSITIONxy.y) & 3;
    
    // Calculate threshold without array
    float threshold;
    int index = x * 4 + y;
    
    // Unroll the array access
    if (index == 0) threshold = 1.0 / 17.0;
    else if (index == 1) threshold = 9.0 / 17.0;
    else if (index == 2) threshold = 3.0 / 17.0;
    else if (index == 3) threshold = 11.0 / 17.0;
    else if (index == 4) threshold = 13.0 / 17.0;
    else if (index == 5) threshold = 5.0 / 17.0;
    else if (index == 6) threshold = 15.0 / 17.0;
    else if (index == 7) threshold = 7.0 / 17.0;
    else if (index == 8) threshold = 4.0 / 17.0;
    else if (index == 9) threshold = 12.0 / 17.0;
    else if (index == 10) threshold = 2.0 / 17.0;
    else if (index == 11) threshold = 10.0 / 17.0;
    else if (index == 12) threshold = 16.0 / 17.0;
    else if (index == 13) threshold = 8.0 / 17.0;
    else if (index == 14) threshold = 14.0 / 17.0;
    else threshold = 6.0 / 17.0;
    
    clip(ditherOpacity - threshold);
}
*/

