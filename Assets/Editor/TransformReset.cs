using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(Transform)), CanEditMultipleObjects]
public class TransformReset : DecoratorEditor
{
    bool unfold = false;

    static Vector3 resetPosition = Vector3.zero;
    static Vector3 resetRotation = Vector3.zero;
    static Vector3 resetScale = Vector3.one;

    SerializedProperty p;
    SerializedProperty r;
    SerializedProperty s;

    public TransformReset() : base("TransformInspector")
    {
    }

    private void OnEnable()
    {
        p = serializedObject.FindProperty("m_LocalPosition");
        r = serializedObject.FindProperty("m_LocalRotation");
        s = serializedObject.FindProperty("m_LocalScale");

        if(EditorPrefs.HasKey("CustomOriginResetPosition"))
        {
            resetPosition = StringToVector3(EditorPrefs.GetString("CustomOriginResetPosition"));
        }
        if(EditorPrefs.HasKey("CustomOriginResetRotation"))
        {
            resetRotation = StringToVector3(EditorPrefs.GetString("CustomOriginResetRotation"));
        }
        if(EditorPrefs.HasKey("CustomOriginResetScale"))
        {
            resetScale = StringToVector3(EditorPrefs.GetString("CustomOriginResetScale"));
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        //add reset 
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Reset All",EditorStyles.miniButton))
        {
            s.vector3Value = resetScale;
            p.vector3Value = resetPosition;
            r.quaternionValue = Quaternion.Euler(resetRotation);
            serializedObject.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }
        if(GUILayout.Button("Position",EditorStyles.miniButtonLeft))
        {
            p.vector3Value = resetPosition;
            serializedObject.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }
        if(GUILayout.Button("Rotation",EditorStyles.miniButtonMid))
        {
            r.quaternionValue = Quaternion.Euler(resetRotation);
            serializedObject.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }
        if(GUILayout.Button("Scale",EditorStyles.miniButtonRight))
        {
            s.vector3Value = resetScale;
            serializedObject.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        string originLabel;
        if(resetPosition == Vector3.zero && resetRotation == Vector3.zero && resetScale == Vector3.one)
        {
            originLabel = "Set Origin [Default]";
        }
        else
        {
            originLabel = "Set Origin [Custom]";
        }

        unfold = EditorGUILayout.Foldout(unfold, originLabel);
    
        if(unfold)
        {
            EditorGUI.BeginChangeCheck();
            if(GUILayout.Button("Set Custom Origin",EditorStyles.miniButtonLeft))
            {
                resetPosition = p.vector3Value;
                resetRotation = r.quaternionValue.eulerAngles;
                resetScale = s.vector3Value;
                GUI.FocusControl(null);
            }
            if(GUILayout.Button("Clear Custom Origin",EditorStyles.miniButtonRight))
            {
                resetPosition = Vector3.zero;
                resetRotation = Vector3.zero;
                resetScale = Vector3.one;
                GUI.FocusControl(null);
            }
            resetPosition = EditorGUILayout.Vector3Field("Position", resetPosition);
            resetRotation = EditorGUILayout.Vector3Field("Rotation", resetRotation);    
            resetScale = EditorGUILayout.Vector3Field("Scale", resetScale);
        
            if(EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("CustomOriginResetPosition", resetPosition.ToString());
                EditorPrefs.SetString("CustomOriginResetRotation", resetRotation.ToString());
                EditorPrefs.SetString("CustomOriginResetScale", resetScale.ToString());
            }
        }
    
    }

    private Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }
}
    
public abstract class DecoratorEditor : Editor
{
    private static readonly object[] EMPTY_ARRAY = new object[0];

    #region Editor Fields

    private System.Type decoratorEditorType;
    private System.Type editedObjectType;
    private Editor editorInstance;

    #endregion
    private static Dictionary<string, MethodInfo> decoratedMethods = new Dictionary<string, MethodInfo>();

    private static Assembly editorAssembly = Assembly.GetAssembly(typeof(Editor));

    protected Editor EditorInstance
    {
        get
        {
            if (editorInstance == null && targets != null && targets.Length > 0)
            {
                editorInstance = Editor.CreateEditor(targets, decoratorEditorType);
            }

            if(editorInstance == null)
            {
                Debug.LogError("Could not create editors!");
            }

            return editorInstance;
        }
    }

    public DecoratorEditor(string editorTypeName)
    {
        this.decoratorEditorType = editorAssembly.GetTypes().Where(t => t.Name == editorTypeName).FirstOrDefault();

        Init();

        var originalEditedType = GetCustomEditorType(decoratorEditorType);

        if(originalEditedType !=editedObjectType)
        {
            throw new System.ArgumentException($"Type {editedObjectType} does not match the editor {editorTypeName}");
        }
    }

    private System.Type GetCustomEditorType(System.Type type)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        var attributes = type.GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
        var field = attributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).First();
        return field.GetValue(attributes[0]) as System.Type;
    }

    private void Init()
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        var attributes = this.GetType().GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
        var field = attributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).First();

        editedObjectType = field.GetValue(attributes[0]) as System.Type;
    }

    private void OnDisable()
    {
        if (editorInstance != null)
        {
            DestroyImmediate(editorInstance);
        }
    }

    protected void CallInspectorMethod(string methodName)
    {
        MethodInfo method = null;

        if(!decoratedMethods.ContainsKey(methodName))
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            
            method = decoratorEditorType.GetMethod(methodName, flags);
        
            if(method != null)
            {
                decoratedMethods[methodName] = method;
            }
            else
            {
                Debug.LogError($"Could not find method {methodName} in {decoratorEditorType}");
            }
        }
        else
        {
            method = decoratedMethods[methodName];
        }

        if(method != null)
        {
            method.Invoke(EditorInstance, EMPTY_ARRAY);
        }
    }

    protected override void OnHeaderGUI()
    {
        CallInspectorMethod("OnHeaderGUI");
    }

    public override void OnInspectorGUI()
    {
        EditorInstance.OnInspectorGUI();
    }

    public override void DrawPreview(Rect previewArea)
    {
        EditorInstance.DrawPreview(previewArea);
    }

    public override string GetInfoString()
    {
        return EditorInstance.GetInfoString();
    }

    public override GUIContent GetPreviewTitle()
    {
        return EditorInstance.GetPreviewTitle();
    }

    public override bool HasPreviewGUI()
    {
        return EditorInstance.HasPreviewGUI();
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {
        EditorInstance.OnInteractivePreviewGUI(r, background);
    }

    public override void OnPreviewGUI(Rect r,GUIStyle backgroud)
    {
        EditorInstance.OnPreviewGUI(r, backgroud);
    }

    public override void OnPreviewSettings()
    {
        EditorInstance.OnPreviewSettings();
    }

    public override void ReloadPreviewInstances()
    {
        EditorInstance.ReloadPreviewInstances();
    }

    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
    {
        return EditorInstance.RenderStaticPreview(assetPath, subAssets, width, height);
    }

    public override bool RequiresConstantRepaint()
    {
        return EditorInstance.RequiresConstantRepaint();
    }

    public override bool UseDefaultMargins()
    {
        return EditorInstance.UseDefaultMargins();
    }
}
