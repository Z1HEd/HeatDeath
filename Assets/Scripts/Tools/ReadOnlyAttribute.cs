using UnityEngine;

/// <summary>
/// Marks a serialized field as visible but non-editable in the inspector.
/// Pair with ReadOnlyDrawer (in an Editor/ folder) to take effect.
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute
{
}