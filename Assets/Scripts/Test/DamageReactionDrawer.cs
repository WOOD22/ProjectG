using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Unit.DamageReactionEntry))]
public class DamageReactionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property == null)
        {
            EditorGUI.HelpBox(position, "Error: Property is null", MessageType.Error);
            EditorGUI.EndProperty();
            return;
        }

        // 레이아웃 계산
        float damageTypeWidth = position.width * 0.4f;
        float reactionWidth = position.width * 0.6f - 5f;

        Rect damageTypeRect = new Rect(position.x, position.y, damageTypeWidth, position.height);
        Rect reactionRect = new Rect(position.x + damageTypeWidth + 5f, position.y, reactionWidth, position.height);

        // DamageType 필드 그리기
        SerializedProperty damageTypeProp = property.FindPropertyRelative("damageType");
        if (damageTypeProp != null)
        {
            EditorGUI.PropertyField(damageTypeRect, damageTypeProp, GUIContent.none);
        }
        else
        {
            EditorGUI.HelpBox(damageTypeRect, "DamageType not found", MessageType.Warning);
        }

        // DamageReaction 필드 그리기
        SerializedProperty reactionProp = property.FindPropertyRelative("reaction");
        if (reactionProp != null)
        {
            EditorGUI.PropertyField(reactionRect, reactionProp, GUIContent.none);
        }
        else
        {
            EditorGUI.HelpBox(reactionRect, "Reaction not found", MessageType.Warning);
        }

        EditorGUI.EndProperty();
    }
}