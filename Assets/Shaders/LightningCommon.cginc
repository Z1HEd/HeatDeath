// Bolt shape functions shared by the lightning shaders. The including shader declares the
// matching properties.
#ifndef LIGHTNING_COMMON_INCLUDED
#define LIGHTNING_COMMON_INCLUDED

float _Amplitude;
float _Frequency;
float _Taper;
float _PixelsPerUnit;

// Sine-free hash, stable for large inputs.
float hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

// Linear (not smooth) interpolation keeps straight segments between sharp kinks.
float valueNoise(float x, float seed)
{
    float i = floor(x);
    float a = hash11(i + seed * 17.13);
    float b = hash11(i + 1 + seed * 17.13);
    return lerp(a, b, frac(x)) * 2 - 1;
}

float boltNoise(float x, float seed)
{
    return valueNoise(x, seed) * 0.6
         + valueNoise(x * 2.3, seed + 13.1) * 0.28
         + valueNoise(x * 5.7, seed + 29.7) * 0.12;
}

// Sideways offset of the bolt at distance t along the hop; eases to 0 at both ends.
float offsetAt(float t, float len, float seed)
{
    float taper = max(min(_Taper, len * 0.5), 1e-4);
    float env = smoothstep(0, taper, t) * smoothstep(0, taper, len - t);
    return boltNoise(t * _Frequency, seed) * _Amplitude * env;
}

// Approximate perpendicular distance to the strand, with round caps past the ends.
float strandDistance(float t, float s, float len, float seed)
{
    const float e = 0.01;
    float tc = clamp(t, 0, len);
    float off = offsetAt(tc, len, seed);
    float slope = (offsetAt(tc + e, len, seed) - offsetAt(tc - e, len, seed)) / (2 * e);
    float d = abs(s - off) / sqrt(1 + slope * slope);
    return length(float2(d, t - tc));
}

// x = core coverage, y = glow falloff.
float2 strand(float d, float coreWidth, float glowWidth)
{
    // Hard edge when pixel snapped, anti-aliased when smooth.
    float aa = _PixelsPerUnit > 0 ? 1e-5 : max(fwidth(d), 1e-5);
    float core = saturate((coreWidth * 0.5 - d) / aa + 0.5);
    float glow = saturate(1 - d / max(glowWidth, 1e-4));
    return float2(core, glow * glow);
}

#endif
