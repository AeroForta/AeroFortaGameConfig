using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Pixelfactor.IP.Engine.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AeroForta
{
    [BepInPlugin("com.aeroforta.globalconfig", "Game Global Config", "1.0.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private bool isWindowOpen = false;
        private bool isSelectingObject = false;

        private Rect windowRect = new Rect(20, 20, 400, 300);
        private Rect selectionDialogRect = new Rect(
            Screen.width / 2 - 150,
            Screen.height / 2 - 100,
            400,
            300
        );

        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 scrollPositionSelection = Vector2.zero;
        private string searchQuery = "";
        private List<string> targetObjectNames = new List<string> { "Idle mode" };
        private int selectedObjectNameIndex = 0;
        private bool foundObject = false;

        private float guiWidth = 600f;
        private float guiHeight = 600f;

        private float dialogGuiWidth = 400f;
        private float dialogGuiHeight = 300f;

        private List<MonoBehaviour> objectSettingsList = new List<MonoBehaviour>();

        private void Awake()
        {
            Logger.LogInfo($"Plugin Game Global Config is loaded!");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                isWindowOpen = !isWindowOpen;
            }
        }

        private void OnGUI()
        {
            if (isWindowOpen)
            {
                windowRect = GUI.Window(0, windowRect, DoMyWindow, "Game Global Config");
            }
            if (isSelectingObject)
            {
                DisplayObjectSelectionDialog();
            }
        }

        private void DoMyWindow(int windowID)
        {
            string selectedObjectName = targetObjectNames[selectedObjectNameIndex];
            GUILayout.Label($"Settings for {selectedObjectName}");

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("GUI Width:");
            float.TryParse(GUILayout.TextField(guiWidth.ToString()), out guiWidth);

            GUILayout.Label("GUI Height:");
            float.TryParse(GUILayout.TextField(guiHeight.ToString()), out guiHeight);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Apply GUI Size"))
            {
                windowRect.width = Mathf.Max(200, guiWidth);
                windowRect.height = Mathf.Max(200, guiHeight);
                windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
                windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
            }
            GUILayout.Label("Search GameObject:");
            searchQuery = GUILayout.TextField(searchQuery);
            if (GUILayout.Button("Search"))
            {
                SearchObjects();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Select Target Object:");

            if (
                GUILayout.Button(
                    $"Select Target Object: {targetObjectNames[selectedObjectNameIndex]}"
                )
            )
            {
                isSelectingObject = true;
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Refresh"))
            {
                GetNonSceneObjects();
            }
            if (GUILayout.Button("Apply")) { }

            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.Width(windowRect.width - 20),
                GUILayout.Height(windowRect.height - 80)
            );

            DisplayObjectSettingsList();

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private void SearchObjects()
        {
            targetObjectNames.Clear();

            string lowerSearchQuery = searchQuery.ToLower();

            GameObject[] foundObjects = Resources
                .FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.name.ToLower().Contains(lowerSearchQuery))
                .ToArray();

            targetObjectNames.AddRange(foundObjects.Select(go => go.name).Distinct());

            if (targetObjectNames.Count > 0)
            {
                selectedObjectNameIndex = 0;
                GetNonSceneObjects();
            }
            else
            {
                targetObjectNames.Add("Can't find anything");
                selectedObjectNameIndex = 0;
                Debug.Log("No objects found with the search query: " + searchQuery);
            }
        }

        private void SelectObjectWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, selectionDialogRect.width, 20));
            GUILayout.BeginHorizontal();
            GUILayout.Label("GUI Width:");
            float.TryParse(GUILayout.TextField(dialogGuiWidth.ToString()), out dialogGuiWidth);

            GUILayout.Label("GUI Height:");
            float.TryParse(GUILayout.TextField(dialogGuiHeight.ToString()), out dialogGuiHeight);
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            if (GUILayout.Button("Apply GUI Size"))
            {
                selectionDialogRect.width = Mathf.Max(200, dialogGuiWidth);
                selectionDialogRect.height = Mathf.Max(200, dialogGuiHeight);
                selectionDialogRect.x = Mathf.Clamp(
                    selectionDialogRect.x,
                    0,
                    Screen.width - selectionDialogRect.width
                );
                selectionDialogRect.y = Mathf.Clamp(
                    selectionDialogRect.y,
                    0,
                    Screen.height - selectionDialogRect.height
                );
            }
            GUILayout.Label($"Found {targetObjectNames.Count} objects.");

            scrollPositionSelection = GUILayout.BeginScrollView(scrollPositionSelection);

            for (int i = 0; i < targetObjectNames.Count; i++)
            {
                bool isToggled = GUILayout.Toggle(
                    i == selectedObjectNameIndex,
                    targetObjectNames[i],
                    "Button"
                );

                if (isToggled && i != selectedObjectNameIndex)
                {
                    selectedObjectNameIndex = i;
                    isSelectingObject = false;
                    GetNonSceneObjects();
                }
            }

            GUILayout.Space(20);

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DisplayObjectSelectionDialog()
        {
            selectionDialogRect = GUI.Window(
                1,
                selectionDialogRect,
                SelectObjectWindow,
                "Select Target Object"
            );
        }

        private void DisplayObjectSettingsList()
        {
            if (objectSettingsList != null && objectSettingsList.Count > 0)
            {
                foreach (var script in objectSettingsList)
                {
                    GUILayout.Label($"Settings for {script.GetType().Name}");
                    DisplayScriptFields(script);
                    GUILayout.Space(40);
                }
            }
            else
            {
                GUILayout.Label("No scripts found!");
            }
        }

        private void ApplySettings()
        {
            if (objectSettingsList != null && objectSettingsList.Count > 0)
            {
                foreach (var script in objectSettingsList)
                {
                    System.Reflection.FieldInfo[] fields = script
                        .GetType()
                        .GetFields(
                            System.Reflection.BindingFlags.Instance
                                | System.Reflection.BindingFlags.Public
                                | System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.FlattenHierarchy
                        );

                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(int) || field.FieldType == typeof(float))
                        {
                            string fieldValueString = GUILayout.TextField(
                                field.GetValue(script).ToString()
                            );
                            int.TryParse(fieldValueString, out int intValue);
                            float.TryParse(fieldValueString, out float floatValue);
                            field.SetValue(
                                script,
                                field.FieldType == typeof(int)
                                    ? (object)intValue
                                    : (object)floatValue
                            );
                        }
                        else if (field.FieldType == typeof(long))
                        {
                            string fieldValueString = GUILayout.TextField(
                                field.GetValue(script).ToString()
                            );
                            long.TryParse(fieldValueString, out long longValue);
                            field.SetValue(script, longValue);
                        }
                        else if (field.FieldType == typeof(double))
                        {
                            string fieldValueString = GUILayout.TextField(
                                field.GetValue(script).ToString()
                            );
                            double.TryParse(fieldValueString, out double doubleValue);
                            field.SetValue(script, doubleValue);
                        }
                        else if (field.FieldType == typeof(bool))
                        {
                            bool fieldValue = (bool)field.GetValue(script);
                            bool newFieldValue = GUILayout.Toggle(fieldValue, "Toggle");
                            field.SetValue(script, newFieldValue);
                        }
                        else if (field.FieldType == typeof(string))
                        {
                            string fieldValue = (string)field.GetValue(script);
                            string newFieldValue = GUILayout.TextField(fieldValue);
                            field.SetValue(script, newFieldValue);
                        }
                        else if (
                            field.FieldType.IsGenericType
                            && field.FieldType.GetGenericTypeDefinition() == typeof(List<>)
                        )
                        {
                            DisplayListField(script, field);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Cannot apply settings as scripts are not found!");
            }
        }

        private void DisplayListField(MonoBehaviour script, System.Reflection.FieldInfo listField)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{listField.Name} (List):");

            var list = (System.Collections.IList)listField.GetValue(script);
            int count = list.Count;
            GUILayout.Label($"Count: {count}");

            GUILayout.EndHorizontal();

            for (int i = 0; i < count; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label($"Element {i}:");

                if (listField.FieldType.GetGenericArguments()[0] == typeof(int))
                {
                    string elementValueString = GUILayout.TextField(list[i].ToString());
                    int.TryParse(elementValueString, out int intValue);
                    list[i] = intValue;
                }
                else if (listField.FieldType.GetGenericArguments()[0] == typeof(float))
                {
                    string elementValueString = GUILayout.TextField(list[i].ToString());
                    float.TryParse(elementValueString, out float floatValue);
                    list[i] = floatValue;
                }
                else if (listField.FieldType.GetGenericArguments()[0] == typeof(long))
                {
                    string elementValueString = GUILayout.TextField(list[i].ToString());
                    long.TryParse(elementValueString, out long longValue);
                    list[i] = longValue;
                }
                else if (listField.FieldType.GetGenericArguments()[0] == typeof(double))
                {
                    string elementValueString = GUILayout.TextField(list[i].ToString());
                    double.TryParse(elementValueString, out double doubleValue);
                    list[i] = doubleValue;
                }
                else if (listField.FieldType.GetGenericArguments()[0] == typeof(bool))
                {
                    bool elementValue = (bool)list[i];
                    bool newElementValue = GUILayout.Toggle(elementValue, "Toggle");
                    list[i] = newElementValue;
                }
                else if (listField.FieldType.GetGenericArguments()[0] == typeof(string))
                {
                    string elementValue = (string)list[i];
                    string newElementValue = GUILayout.TextField(elementValue);
                    list[i] = newElementValue;
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DisplayScriptFields(MonoBehaviour script)
        {
            System.Reflection.FieldInfo[] fields = script
                .GetType()
                .GetFields(
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.FlattenHierarchy
                );
            fields = fields.OrderBy(field => field.Name).ToArray();
            foreach (var field in fields)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label($"{field.Name}:");

                if (field.FieldType == typeof(int) || field.FieldType == typeof(float))
                {
                    string fieldValueString = GUILayout.TextField(
                        field.GetValue(script).ToString()
                    );
                    int.TryParse(fieldValueString, out int intValue);
                    float.TryParse(fieldValueString, out float floatValue);
                    field.SetValue(
                        script,
                        field.FieldType == typeof(int) ? (object)intValue : (object)floatValue
                    );
                }
                else if (field.FieldType == typeof(long))
                {
                    string fieldValueString = GUILayout.TextField(
                        field.GetValue(script).ToString()
                    );
                    long.TryParse(fieldValueString, out long longValue);
                    field.SetValue(script, longValue);
                }
                else if (field.FieldType == typeof(double))
                {
                    string fieldValueString = GUILayout.TextField(
                        field.GetValue(script).ToString()
                    );
                    double.TryParse(fieldValueString, out double doubleValue);
                    field.SetValue(script, doubleValue);
                }
                else if (field.FieldType == typeof(bool))
                {
                    bool fieldValue = (bool)field.GetValue(script);
                    bool newFieldValue = GUILayout.Toggle(fieldValue, "Toggle");
                    field.SetValue(script, newFieldValue);
                }
                else if (field.FieldType == typeof(string))
                {
                    string fieldValue = (string)field.GetValue(script);
                    string newFieldValue = GUILayout.TextField(fieldValue);
                    field.SetValue(script, newFieldValue);
                }
                else if (
                    field.FieldType.IsGenericType
                    && field.FieldType.GetGenericTypeDefinition() == typeof(List<>)
                )
                {
                    DisplayListField(script, field);
                }

                GUILayout.EndHorizontal();
            }
        }

        private void GetNonSceneObjects()
        {
            objectSettingsList.Clear();

            string selectedObjectName = targetObjectNames[selectedObjectNameIndex];
            foreach (
                GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[]
            )
            {
                if (go.name == selectedObjectName && !IsHidden(go.hideFlags))
                {
                    MonoBehaviour[] scripts = go.GetComponents<MonoBehaviour>();
                    objectSettingsList.AddRange(scripts);

                    if (scripts.Length > 0)
                    {
                        foundObject = true;
                        Debug.Log("Found the GameObject with name: " + selectedObjectName);
                    }
                }
            }

            if (!foundObject)
            {
                Debug.Log("Did not find the GameObject with name: " + selectedObjectName);
            }
        }

        private bool IsHidden(HideFlags flags)
        {
            return flags == HideFlags.NotEditable || flags == HideFlags.HideAndDontSave;
        }
    }
}
