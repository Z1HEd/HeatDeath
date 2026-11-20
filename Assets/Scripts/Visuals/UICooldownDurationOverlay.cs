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

    // The share of the cooldown left: the overlay covers that much of the button.
    public void SetCooldownFill(float cooldownLeft)
    {
        _cooldownProgress = Mathf.Clamp01(cooldownLeft);
        _cooldownOverlay.MarkDirtyRepaint();
    }
    // The share of the duration left.
    public void SetDurationFill(float durationLeft)
    {
        _durationProgress = Mathf.Clamp01(durationLeft);
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

    // A clock: the covered part shrinks as its hand sweeps clockwise from 12 o'clock, uncovering the button.
    // The element clips the pie to its square.
    private void DrawRadialFill(Painter2D painter, Rect rect, float progress)
    {
        if (progress >= 1f)
        {
            DrawVerticalFill(painter, rect, 1f);
            return;
        }

        Vector2 center = rect.center;
        // Past the corners, so the pie reaches the square's edges.
        float radius = Mathf.Max(rect.width, rect.height);
        float hand = -90f + 360f * (1f - progress);

        painter.BeginPath();
        painter.MoveTo(center);
        painter.Arc(center, radius, hand, 270f, ArcDirection.Clockwise);
        painter.ClosePath();
        painter.Fill();
    }
}