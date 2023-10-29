using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
#if UNITY_EDITOR
[CustomEditor(typeof(DataMigrationSettings))]
public class DataMigrationSettingsEditor : Editor
{
    public SerializedProperty spreadsheetID;
    public SerializedProperty importSheetID;
    public SerializedProperty exportSheetID;
    public SerializedProperty columnsToRead;
    public SerializedProperty saveSOToPath;
    public SerializedProperty loadAudioPath;
    public SerializedProperty sentResultByEmail;
    public SerializedProperty researchersEmail;
    public SerializedProperty newSheetProperties;

    public IList<Dictionary<string, string>> pulledData = null;
    public void OnEnable()
    {
        spreadsheetID = serializedObject.FindProperty("spreadsheetID");
        importSheetID = serializedObject.FindProperty("importSheetID");
        exportSheetID = serializedObject.FindProperty("exportSheetID");
        columnsToRead = serializedObject.FindProperty("columnsToRead");
        saveSOToPath = serializedObject.FindProperty("saveSOToPath");
        loadAudioPath = serializedObject.FindProperty("loadAudioPath");
        sentResultByEmail = serializedObject.FindProperty("sentResultByEmail");
        researchersEmail = serializedObject.FindProperty("researchersEmail");
        newSheetProperties = serializedObject.FindProperty("newSheetProperties");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (GUILayout.Button(new GUIContent("Load Credentials")))
        {
            // open file explorer and enable to choose a json file 
            var file = EditorUtility.OpenFilePanel("Load the credentials from a json file", "", "json");

            if (!string.IsNullOrEmpty(file))
            {
                ProcessServiceAccountKey(file);
            }
            else
                Debug.LogError("No credential json file was selected");
        }

        #region IMPORT
        // display only if google sheet porvided is plugged in
        EditorGUILayout.PropertyField(spreadsheetID, new GUIContent("Spreadsheet ID"));
        using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(spreadsheetID.stringValue)))
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            // display only is spreadsheet string id is there

            EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(importSheetID, new GUIContent("Import Sheet ID"));
            using (new EditorGUI.DisabledGroupScope(importSheetID.intValue == 0))
            {
                if (GUILayout.Button(new GUIContent("Open Meta Data Table")))
                {
                    GoogleSheets.OpenSheetInBrowser(spreadsheetID.stringValue, importSheetID.intValue);
                }
                if (GUILayout.Button(new GUIContent("Import and save data")))
                {
                    pulledData = GoogleSheets.PullData(spreadsheetID.stringValue, importSheetID.intValue, true, columnsToRead.intValue);
                    if (pulledData == null)
                    {
                        Debug.LogError("Data was pulled incorrectly");
                    }
                    else
                    {
                        GenerateTracksData.GenerateData(pulledData, (DataMigrationSettings)target);
                    }
                }
                EditorGUILayout.PropertyField(columnsToRead, new GUIContent("Columns to Read"));
                EditorGUILayout.PropertyField(loadAudioPath, new GUIContent("Path to load audio tracks from"));
                EditorGUILayout.PropertyField(saveSOToPath, new GUIContent("Saved Data Path"));
            }
            #endregion

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            #region EXPORT
            EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledGroupScope(exportSheetID.intValue != 0))
            {
                EditorGUILayout.PropertyField(newSheetProperties, new GUIContent("New spreadsheet properties"));
                if (GUILayout.Button(new GUIContent("Create Collected Data Table")))
                {
                    int newSheetID = CreateNewSheet(((DataMigrationSettings)target).exportSheetName);
                    if (newSheetID != 0)
                        exportSheetID.intValue = newSheetID;
                    exportSheetID.serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();

                }
            }
            using (new EditorGUI.DisabledGroupScope(exportSheetID.intValue == 0))
            {
                if (GUILayout.Button(new GUIContent("Open Collected Data Table")))
                {
                    GoogleSheets.OpenSheetInBrowser(spreadsheetID.stringValue, exportSheetID.intValue);
                }
                EditorGUILayout.PropertyField(exportSheetID, new GUIContent("Export Sheet ID"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(sentResultByEmail, new GUIContent("Send Collected Data to Email"));
            using (new EditorGUI.DisabledGroupScope(!sentResultByEmail.boolValue))
            {
                EditorGUILayout.PropertyField(researchersEmail, new GUIContent("Email to send to"));
            }
            #endregion
        }
        serializedObject.ApplyModifiedProperties();
    }


    int CreateNewSheet(string sheetName)
    {
        try
        {
            EditorUtility.DisplayProgressBar("Add Sheet", string.Empty, 0);
            // return the id of the created sheet
            return GoogleSheets.AddSheet(spreadsheetID.stringValue, sheetName, ((DataMigrationSettings)target).newSheetProperties);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        return 0;

    }
    public void CreateAssetFolder(string folderPath) // create a place to save our scriptable object
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Console.WriteLine("Directory created: " + folderPath);
        }
        else // I am deleting everything where our asset will be placed to ensure no old  data remains 
        {

            DirectoryInfo directory = new DirectoryInfo(folderPath);

            // Delete all files in the folder
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
            // make changes appear in the editor
            AssetDatabase.Refresh();
        }
    }
    public void ProcessServiceAccountKey(string path)
    {
        // encrypt it 
        // save it to streaming assets folder so we can access it in runtime in builds
        try
        {
            SaveJsonCredentials(path);
            Debug.Log("Processing of service account key was successful.");
        }
        catch (IOException e)
        {
            Debug.LogError($"Service account credential file is not found. Please follow instructions on {SheetsServiceProvider.instructionLocation} and try again.");
            Debug.LogError(e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError("Processing of service account key was not successful. " + e.Message);
        }
    }
    private void SaveJsonCredentials(string path)
    {
        string jsonString = File.ReadAllText(path);
        Template temp = JsonUtility.FromJson<Template>(jsonString);
        ResearcherData researcherData = ScriptableObject.CreateInstance<ResearcherData>();
        researcherData.LoadData(temp.type, temp.project_id, temp.client_email, temp.client_id, temp.private_key, temp.private_key_id);
        string saveName = "Data1.asset";
        CreateAssetFolder(SheetsServiceProvider.savePath);
        AssetDatabase.CreateAsset(researcherData, Path.Combine(SheetsServiceProvider.savePath, saveName));
        AssetDatabase.SaveAssets();

    }
}

[Serializable]
public class Template // this class is used as a deserialize template for the json key (data shall not be saved here, it is temporary). 
{
    public string type;
    public string project_id;
    public string private_key_id;
    public string private_key;
    public string client_email;
    public string client_id;
    public string auth_uri;
    public string token_uri;
    public string auth_provider_x509_cert_url;
    public string client_x509_cert_url;
    public string universe_domain;
}
#endif