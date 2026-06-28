using UnityEngine;
using UnityEngine.UIElements;

public class UICooldownDurationOverlay
{
    public Button button;
    private VisualElement _cooldownOverlay;
    private Color _cooldownColor;
    private VisualElement _durationOverlay;
    private Color _durationColor;
    private float _cooldownProgress = 0f; // 0 = ready, 1 = full cooldown
    private float _durationProgress = 0f; // 0 = ran out, 1 = full duration

    public bool isRadialFill = true;

    public UICooldownDurationOverlay(Button _button)
    {
        button = _button;
        _cooldownOverlay = _button.Q<VisualElement>("Cooldown");
        _durationOverlay = _button.Q<VisualElement>("Duration");
        if (_cooldownOverlay != null){
            _cooldownColor = _cooldownOverlay.resolvedStyle.backgroundColor;
            _cooldownOverlay.generateVisualContent += OnGenerateCooldownMesh;
            _cooldownOverlay.style.backgroundColor = Color.clear;
        }
        if (_durationOverlay != null){
            _durationColor = _durationOverlay.resolvedStyle.backgroundColor;
            _durationOverlay.generateVisualContent += OnGenerateDurationMesh;
            _durationOverlay.style.backgroundColor = Color.clear;
        } 
    }

    public void SetCooldownFill(float progress)
    {
        _cooldownProgress = Mathf.Clamp01(1f-progress);
        _cooldownOverlay.MarkDirtyRepaint(); 
    }
    public void SetDurationFill(float progress)
    {
        _durationProgress = Mathf.Clamp01(1f-progress);
        _durationOverlay.MarkDirtyRepaint(); 
    }
    public void SetIcon(Texture2D texture)
    {
        button.style.backgroundImage = 
                new StyleBackground(texture);
    }
    public void SetClickable(bool clickable)
    {
        button.pickingMode = clickable ? PickingMode.Position : PickingMode.Ignore;
    }

    private void OnGenerateCooldownMesh(MeshGenerationContext mgc)
    {
        if (_cooldownProgress <= 0.005f) return;

        var painter = mgc.painter2D;
        Rect rect = _cooldownOverlay.contentRect;

        painter.fillColor = _cooldownColor; 

        if (isRadialFill)
            DrawRadialFill(painter, rect, _cooldownProgress);
        else
            DrawVerticalFill(painter, rect, _cooldownProgress);
    }

    private void OnGenerateDurationMesh(MeshGenerationContext mgc)
    {
        if (_durationProgress <= 0.005f) return;

        var painter = mgc.painter2D;
        Rect rect = _durationOverlay.contentRect;

        painter.fillColor = _durationColor; 

        if (isRadialFill)
            DrawRadialFill(painter, rect, _durationProgress);
        else
            DrawVerticalFill(painter, rect, _durationProgress); 
    }

    private void DrawVerticalFill(Painter2D painter, Rect rect, float progress)
    {
        float fillHeight = rect.height * progress;
        float topY = rect.height - fillHeight;

        painter.BeginPath();
        painter.MoveTo(new Vector2(0, topY));
        painter.LineTo(new Vector2(rect.width, topY));
        painter.LineTo(new Vector2(rect.width, rect.height));
        painter.LineTo(new Vector2(0, rect.height));
        painter.ClosePath();
        painter.Fill();
    }

    private void DrawRadialFill(Painter2D painter, Rect rect, float progress)
    {
        if (progress == 1.0f || progress == 0.0f) return;
        Vector2 center = rect.center;
        
        // Ensure the radius cleanly matches the longest dimension to reach corners
        float radius = Mathf.Max(rect.width, rect.height) * 1.5f; 

        painter.BeginPath();
        painter.MoveTo(center);

        float startAngle = -90f;
        float endAngle = startAngle + (360f * progress);

        painter.Arc(center, radius, startAngle, endAngle, ArcDirection.CounterClockwise);
        painter.LineTo(center);
        painter.ClosePath();
        painter.Fill();
    }
}