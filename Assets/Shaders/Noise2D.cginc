// 2D value noise for the procedural effect shaders (toxic cloud, nanite swarm).
#ifndef NOISE_2D_INCLUDED
#define NOISE_2D_INCLUDED

float hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Smooth value noise, 0 to 1.
float valueNoise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3 - 2 * f);
    float a = hash21(i);
    float b = hash21(i + float2(1, 0));
    float c = hash21(i + float2(0, 1));
    float d = hash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Four octaves, about 0 to 1.
float fbm2(float2 p)
{
    float value = 0;
    float amplitude = 0.5;
    for (int i = 0; i < 4; i++)
    {
        value += amplitude * valueNoise2(p);
        p = p * 2.03 + 17.1;
        amplitude *= 0.5;
    }
    return value / 0.9375;
}

#endif
