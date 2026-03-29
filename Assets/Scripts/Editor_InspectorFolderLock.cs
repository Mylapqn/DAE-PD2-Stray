using UnityEngine;
using UnityEditor;
using System.IO;

//Code by Albarnie

[InitializeOnLoad]
public class Editor_InspectorFolderLock
{
	//The last object we had selected
	static Object lastSelectedObject;
	//Object to select the next frame
	static Object objectToSelect;
	//Whether we have locked the inspector
	static bool isInspectorLockedByUs;

	static Editor_InspectorFolderLock()
	{
		Selection.selectionChanged += OnSelectionChanged;
		EditorApplication.update += OnUpdate;

		//Restore folder lock status
		isInspectorLockedByUs = EditorPrefs.GetBool("FolderSelectionLocked", false);
	}

	static void OnSelectionChanged()
	{
		if (lastSelectedObject != null && Selection.activeObject != null)
		{
			//If the selection has actually changed
			if (Selection.activeObject != lastSelectedObject)
			{
				//If the new object is a folder, reselect our old object
				if (IsAssetAFolder(Selection.activeObject))
				{
					//We have to select the object the next frame, otherwise it will not register
					objectToSelect = lastSelectedObject;
				}
				else
				{
					UnLockInspector();
					//Update the last object
					lastSelectedObject = Selection.activeObject;
				}
			}
		}
		else if (!IsAssetAFolder(Selection.activeObject))
		{
			lastSelectedObject = Selection.activeObject;
			UnLockInspector();
		}

	}

	//We have to do selecting in the next editor update because Unity does not allow selecting another object in the same editor update
	static void OnUpdate()
	{
		//If the editor is locked then we don't care
		if (objectToSelect != null && !ActiveEditorTracker.sharedTracker.isLocked)
		{
			//Select the new object
			Selection.activeObject = objectToSelect;

			LockInspector();

			lastSelectedObject = objectToSelect;
			objectToSelect = null;
		}
		else
		{
			objectToSelect = null;
		}
	}

	static void LockInspector()
	{
		ActiveEditorTracker.sharedTracker.isLocked = true;
		isInspectorLockedByUs = true;
		//We store the state so that if we compile or leave the editor while the folders are locked then the state is kept
		EditorPrefs.SetBool("FolderSelectionLocked", true);
	}

	static void UnLockInspector()
	{
		//Only unlock inspector if we are the one who locked it
		if (isInspectorLockedByUs)
		{
			ActiveEditorTracker.sharedTracker.isLocked = false;
			isInspectorLockedByUs = false;
			EditorPrefs.SetBool("FolderSelectionLocked", false);
		}
	}

	private static bool IsAssetAFolder(Object obj)
	{
		string path = "";

		if (obj == null)
		{
			return false;
		}

		//Get the path to the asset
		path = AssetDatabase.GetAssetPath(obj);

		//If the asset is a directory (i.e a folder)
		if (path.Length > 0 && Directory.Exists(path))
		{
			return true;
		}

		return false;
	}

}
