using UnityEngine;
using UnityEngine.UIElements;

public class UICooldownOverlay
{
    private VisualElement _overlayElement;
    private float _cooldownProgress = 0f; // 0 = ready, 1 = full cooldown

    public bool isRadialFill = true;

    public UICooldownOverlay(VisualElement overlayElement)
    {
        _overlayElement = overlayElement;
        // Hook into the vector drawing API
        _overlayElement.generateVisualContent += OnGenerateVisualContent;
    }

    // Call this from your Weapon script every frame (0.0 to 1.0)
    public void SetUpdateFill(float progress)
    {
        _cooldownProgress = Mathf.Clamp01(progress);
        
        // Force the element to redraw itself
        _overlayElement.MarkDirtyRepaint(); 
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (_cooldownProgress >= 1f) return;
        var painter = mgc.painter2D;
        Rect rect = _overlayElement.contentRect;

        // Set the tint color of your overlay mask
        painter.fillColor = new Color(0, 0, 0, 0.6f); 

        if (isRadialFill)
            DrawRadialFill(painter, rect);
        else
            DrawVerticalFill(painter, rect);
    }

    // OPTION A: Linear Top-to-Bottom Wipe (Fixed using path coordinates)
    private void DrawVerticalFill(Painter2D painter, Rect rect)
    {
        float fillHeight = rect.height * (1f-_cooldownProgress);
        float topY = rect.height - fillHeight;

        painter.BeginPath();
        painter.MoveTo(new Vector2(0, topY));
        painter.LineTo(new Vector2(rect.width, topY));
        painter.LineTo(new Vector2(rect.width, rect.height));
        painter.LineTo(new Vector2(0, rect.height));
        painter.ClosePath();
        painter.Fill();
    }

    // OPTION B: Clock/Radial Fill (Fixed using ArcDirection)
    private void DrawRadialFill(Painter2D painter, Rect rect)
    {
        Vector2 center = rect.center;
        // Ensure the radius covers the entire element corners
        float radius = Mathf.Max(rect.width, rect.height); 

        painter.BeginPath();
        painter.MoveTo(center);

        // 0 degrees is Right (3 o'clock). 
        // We start at -90 degrees (12 o'clock) and sweep clockwise.
        float startAngle = -90f;
        float endAngle = startAngle + (360f * _cooldownProgress);

        painter.Arc(center, radius, startAngle, endAngle, ArcDirection.CounterClockwise);
        painter.LineTo(center);
        painter.ClosePath();
        painter.Fill();
    }
}