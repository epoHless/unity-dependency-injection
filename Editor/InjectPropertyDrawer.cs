using UnityEditor;
using UnityEngine;

namespace epoHless.DependencyInjection.Editor
{
    [CustomPropertyDrawer(typeof(InjectAttribute))]
    public class InjectPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = new Rect(position.x, position.y, 20, 20);
            position.xMin += 24;
           
            var savedColor = GUI.color;
            var isNull = property.objectReferenceValue == null;
            
            GUI.color = isNull ? Color.red : Color.green;
            GUI.Label(rect,  isNull ? "[X]" : "[O]");
            GUI.color = savedColor;
            
            EditorGUI.PropertyField(position, property, label);
        }
    }
}