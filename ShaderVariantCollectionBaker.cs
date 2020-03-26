using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ShaderVariantCollectionBaker : EditorWindow {

    const string builtInShaderSrcPath = "Assets/Art/Resources/internal";
    const string shaderVariantCollectionPath = "Assets/Art/Resources/internal/shaderVariantCollection.asset";
    static ShaderVariantCollectionBaker _instance = null;
    [MenuItem("Tools/ShaderVariantCollection")]
    public static void CreateWin()
    {
        if(_instance != null)
        {
            _instance.Close();
            _instance = null;
        }
       _instance = CreateInstance<ShaderVariantCollectionBaker>();
        _instance.Show();
    }

    List<string> SelectedFolder = new List<string>();
    ShaderVariantCollection shaderVariantCollection = null;
    private void OnInspectorUpdate()
    {
        
    }


    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        if(shaderVariantCollection == null)
        {
            
            if (shaderVariantCollection == null)
            {
                shaderVariantCollection = new ShaderVariantCollection();
            }

        }
        for(int i = 0, c = SelectedFolder.Count; i < c; i++)
        {
            SelectedFolder[i] = AssetDatabase.GetAssetPath( EditorGUILayout.ObjectField(AssetDatabase.LoadMainAssetAtPath(SelectedFolder[i]), typeof(Object)));
        }
        if(GUILayout.Button("Add"))
        {
            SelectedFolder.Add(string.Empty);
        }
        

        if (GUILayout.Button("Collect variants"))
        {
            Dictionary<string, List<string>> shaderKeywordSupport = new Dictionary<string, List<string>>();
            Dictionary<string, UnityEngine.Rendering.PassType> shaderPassType = new Dictionary<string, UnityEngine.Rendering.PassType>();
            var materials = AssetDatabase.FindAssets("t:Material", SelectedFolder.ToArray());
            shaderVariantCollection.Clear();
            for (int i = 0, c = materials.Length; i < c; i++)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materials[i]));
                var shader = mat.shader;
                //Shader shaderNew = Instantiate(shader);
                //shaderNew.name = "test123";
                //AssetDatabase.CreateAsset(shaderNew, "Assets/shader.asset");
                var shaderPath = AssetDatabase.GetAssetPath(shader);
                UnityEngine.Rendering.PassType passType =  UnityEngine.Rendering.PassType.ForwardBase;
                if (shaderPath.StartsWith(builtInShaderSrcPath))
                {
                    var shaderGuid = AssetDatabase.AssetPathToGUID(shaderPath);
                    if (!shaderKeywordSupport.ContainsKey(shaderGuid))
                    {
                        shaderKeywordSupport[shaderGuid] = new List<string>();
                        UnityEditor.SerializedObject so = new SerializedObject(shader);
                        var parsedForm = so.FindProperty("m_ParsedForm");
                        var subShaders = parsedForm.FindPropertyRelative("m_SubShaders");
                        var passes = subShaders.GetArrayElementAtIndex(0).FindPropertyRelative("m_Passes");
                        //passType= passes.GetArrayElementAtIndex(0).FindPropertyRelative("m_Type").intValue;
                        var firstTag = passes.GetArrayElementAtIndex(0).FindPropertyRelative("m_State").FindPropertyRelative("m_Tags").FindPropertyRelative("tags").
                            GetArrayElementAtIndex(0);

                        var tagIt = firstTag.GetEnumerator();
                        tagIt.MoveNext();
                        bool isLightMode = (tagIt.Current as SerializedProperty).stringValue == "LIGHTMODE";
                        tagIt.MoveNext();
                        var lightMode = (tagIt.Current as SerializedProperty).stringValue;
                        lightMode = lightMode.ToUpper();
                        // Debug.Log(lightMode);
                        if (isLightMode) {
                            if (lightMode == "FORWARDBASE")
                            {
                                passType = UnityEngine.Rendering.PassType.ForwardBase;
                            }
                            else if (lightMode == "DEFERRED")
                            {
                                passType = UnityEngine.Rendering.PassType.Deferred;
                            }
                            else if (lightMode == "FORWARDADD")
                            {
                                passType = UnityEngine.Rendering.PassType.ForwardAdd;
                            }
                            else if (lightMode == "SHADOWCASTER")
                            {
                                passType = UnityEngine.Rendering.PassType.ShadowCaster;
                            }
                            else if (lightMode == "META")
                            {
                                passType = UnityEngine.Rendering.PassType.Meta;
                            }
                            else
                            {
                                passType = UnityEngine.Rendering.PassType.ForwardBase;
                            }
                        }
                        else
                        {
                            passType = UnityEngine.Rendering.PassType.Normal;
                        }
                        //var passType = subShader.FindPropertyRelative("m_SubShaders").GetArrayElementAtIndex(0).GetArrayElementAtIndex(0).FindPropertyRelative("m_Type");
                        //if (passType != null)
                        //{
                        //    Debug.Log("passType:" + passType.intValue);
                        //}
                        var it = so.GetIterator();
                        while (it.Next(true))
                        {
                            var prop = it;
                            if (prop.name == "m_BuiltinKeywords")
                            {
                                if (prop.isArray)
                                {
                                    // Debug.Log(prop.stringValue);
                                    var keywords = prop.stringValue.Split(" ");
                                    shaderKeywordSupport[shaderGuid].AddRange(keywords);
                                    //Debug.Log("m_BuiltinKeywords");
                                }
                            }
                            if (prop.name == "m_NonStrippedUserKeywords")
                            {
                                if (prop.isArray)
                                {
                                    // Debug.Log(prop.stringValue);
                                    var keywords = prop.stringValue.Split(" ");
                                    shaderKeywordSupport[shaderGuid].AddRange(keywords);

                                }
                            }

                            if(prop.name == "m_VariantsUser0")
                            {
                                if (prop.isArray)
                                {
                                    for(int propIdx=0, propC = prop.arraySize; propIdx < propC; propIdx++)
                                    {
                                        var propVariant = prop.GetArrayElementAtIndex(propIdx);
                                        if (propVariant.isArray) {
                                            for (int childPropIdx = 0, childPropC = propVariant.arraySize; childPropIdx < childPropC ; childPropIdx++) {
                                                var variantName = propVariant.GetArrayElementAtIndex(childPropIdx).stringValue;
                                                // Debug.Log(variantName);
                                                shaderKeywordSupport[shaderGuid].Add(variantName);
                                            }
                                        }
                                    }
                                }
                            }
                            if (prop.name == "keywordName")
                            {
                               // Debug.Log(prop.stringValue);
                                shaderKeywordSupport[shaderGuid].Add(prop.stringValue);
                            }


                            //if (prop.name == "m_CompileInfo")
                            //{
                            //    Debug.Log("m_CompileInfo");
                            //}
                        }
                        shaderPassType[shaderGuid] = passType;
                    }
                    

                    List<string> matKeywords = new List<string>();
                    for (int matIdx = 0, matc = mat.shaderKeywords.Length; matIdx < matc; matIdx++)
                    {
                        var shaderMacro = mat.shaderKeywords[matIdx];
                        if (!string.IsNullOrEmpty(shaderMacro) && shaderKeywordSupport[shaderGuid].Contains(shaderMacro))
                        {
                            matKeywords.Add(shaderMacro);
                        }
                    }

                    if(matKeywords.Count > 0)
                    {
                        matKeywords.Add("DIRECTIONAL");
                    }
                    //matKeywords.Add(shaderPassType[shaderGuid].ToString());

                    foreach(var micro in matKeywords)
                    {
                        Debug.Log("$$ macro:" + micro);
                    }
                    ShaderVariantCollection.ShaderVariant shaderVariant = new ShaderVariantCollection.ShaderVariant(
                        shader, shaderPassType[shaderGuid], matKeywords.ToArray()
                        );

                    shaderVariantCollection.Add(shaderVariant);
                }
            }

            
            //Debug.Log(AssetDatabase.GetAssetPath(shaderVariantCollection));
            if (AssetDatabase.GetAssetPath(shaderVariantCollection) != "")
            {
                AssetDatabase.SaveAssets();
            }
            else
            {
                var filePath = UnityEditor.EditorUtility.SaveFilePanelInProject("save variant", "shaderVariantCollection", "asset", "Ok");
                AssetDatabase.CreateAsset(shaderVariantCollection, filePath);
            }

        }

        if (GUILayout.Button("Load"))
        {
            var filePath = UnityEditor.EditorUtility.OpenFilePanelWithFilters("load variant", "Assets", new string[] { "ShaderVariantCollection", "asset" });
            shaderVariantCollection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(filePath);
        }
        EditorGUILayout.EndVertical();
    }

    private bool isBuiltInShaderSource(string shaderPath)
    {
        return shaderPath.StartsWith(builtInShaderSrcPath);
    }
}
