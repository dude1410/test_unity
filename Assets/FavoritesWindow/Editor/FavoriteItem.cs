using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Favorites
{
	public class FavoriteItem : IDraggable
	{
		private AbstractAssetDatabase AssetDatabase;
		private const string DontDestroyOnLoadScene = "DontDestroyOnLoad";

		public static bool IsSceneRef(
			UnityEngine.Object reference, 
			string diskPath, 
			AbstractAssetDatabase assetDatabase)
		{
			var rootReference = assetDatabase.LoadAssetAtPath<UnityEngine.Object>(diskPath);

			bool isRootReference = (reference == rootReference);
			bool isDontDestroyOnLoadScene = (diskPath == DontDestroyOnLoadScene);

			return !isRootReference && (rootReference is SceneAsset || isDontDestroyOnLoadScene);
		}


		public string guid;
		private string cachedPath;
		private long localId;
		private List<int> pathCoordinates;
        public bool isMissingReference = false;
		private string diskFilePath;
		private string virtualFilePath;
		private string fullVirtualAssetPath;
		private string errorMessage;
		private UnityEngine.Object asset;
		private bool initialized = false;

		private int instanceId = 0;

		public FavoriteItem( 
			string guid, string cachedPath, long localId, List<int> pathCoordinates,
			AbstractAssetDatabase assetDatabase )
		{
			this.guid = guid;
			this.cachedPath = cachedPath;
			this.localId = localId;
			this.pathCoordinates = pathCoordinates;
			this.AssetDatabase = assetDatabase;
		}

		public FavoriteItem( 
			UnityEngine.Object reference,
			string diskPath ,
			AbstractAssetDatabase assetDatabase)
		{
			this.AssetDatabase = assetDatabase;
			var rootReference = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(diskPath);
			this.guid = AssetDatabase.AssetPathToGUID(diskPath);
			this.localId = -1;
			this.pathCoordinates = new List<int>();
			this.instanceId = AssetDatabase.GetInstanceId(reference);

			bool isDontDestroyOnLoadScene = (diskPath == DontDestroyOnLoadScene);

			if( reference == rootReference) // IsAsset
			{
				// Anything else??
			}
			else if ( rootReference is SceneAsset || isDontDestroyOnLoadScene) // IsSceneRef
			{
				this.pathCoordinates = AssetDatabase.GetSceneRefCoords(reference);  
				this.cachedPath = AssetDatabase.GetSceneRefFullPath(reference);
				if(isDontDestroyOnLoadScene)
					this.guid = DontDestroyOnLoadScene;	
			}
			else // IsSubAsset
			{
				string _guid;
				long localId;
				if(AssetDatabase.TryGetLocalIdAndGuid(reference, out localId, out _guid))
				{
					this.localId = localId;
				}	
			}			
		}

		public long LocalId 
		{
			get { return this.localId;}
		}

		public int InstanceId
		{
			get { return this.instanceId; }
			set { this.instanceId = value; }
		}

		public List<int> PathCoordinates
		{
			get { return this.pathCoordinates;}
		}

		public string ErrorMessage
		{
			get 
			{
				EnsureInitialized(); 
				return this.errorMessage;
			}
		}

		public bool IsAsset
		{
			get
			{
				return !string.IsNullOrEmpty(guid) && localId == -1 && pathCoordinates.Count == 0;
			}
		}

		public bool IsSceneReference
		{
			get
			{
				return !string.IsNullOrEmpty(guid) && localId == -1 && pathCoordinates.Count > 0;
			}
		}

		public bool IsSubAsset
		{
			get
			{
				return !string.IsNullOrEmpty(guid) && localId > -1 && pathCoordinates.Count == 0;
			}
		}

        public UnityEngine.Object Asset
		{
			get
			{
				EnsureInitialized();
				return this.asset;
			}
		}

		public string AssetPath
		{
			get
			{
				EnsureInitialized();
				return this.diskFilePath;
			}
		}

		public string SubAssetPath
		{
			get
			{
				EnsureInitialized();
				return this.virtualFilePath;
			}
		}

		public string SceneRefGuid { get; set; }

		public string DisplayPath { get; set; }
        public string FullVirtualPath 
		{ 
			get
			{
				EnsureInitialized();
				return this.fullVirtualAssetPath;
			}
		}

        public bool ShouldReset(bool projectDidUpdate, bool hierarchyDidUpdate)
        {
			if( IsAsset )
			{
				string currentPath = AssetDatabase.GUIDToAssetPath(guid);
				string lastKnownPath = AssetPath;
				bool assetExists = AssetDatabase.Exists(currentPath);

				return (assetExists && isMissingReference)
					||(!assetExists && !isMissingReference)
					||(currentPath != lastKnownPath);
			}
			else if ( IsSubAsset && projectDidUpdate )
			{
				return isMissingReference 
					|| !asset 
					|| SubAssetPath != Asset.name;
			}
			else if ( IsSceneReference && 
				( 
					projectDidUpdate 
					|| hierarchyDidUpdate 
					|| 
					(
						isMissingReference 
						&& AssetDatabase.InstanceIDToObject(InstanceId) != null
					) 
				)
			)
			{
				return isMissingReference
					|| !Asset
					|| AssetDatabase.GetSceneRefFullPath(Asset) != FullVirtualPath
					|| !AssetDatabase.GetSceneRefCoords(Asset).SequenceEqual(PathCoordinates);
			}

			return false;
        }

        public void Reset()
		{
			if ( !string.IsNullOrEmpty( FullVirtualPath ) )
				cachedPath = fullVirtualAssetPath;

			if( this.IsSceneReference )
			{
				asset = AssetDatabase.InstanceIDToObject(instanceId);
				if( asset != null)
                {
                    ResetSceneRefFromInstance(asset);
                }
            }

			isMissingReference = false;
			diskFilePath = null;
			virtualFilePath = null;
			DisplayPath = null;
			fullVirtualAssetPath = null;
			errorMessage = null;
			asset = null;
			initialized = false;
		}

        private void ResetSceneRefFromInstance(UnityEngine.Object sceneRef)
        {
            pathCoordinates = AssetDatabase.GetSceneRefCoords(sceneRef);
            cachedPath = AssetDatabase.GetSceneRefFullPath(sceneRef);
            // Scene ref can change to a different scene
            // eg: going to dont destroy on load or move to 
            // different scene through script
            string rootAssetPath = AssetDatabase.GetAssetPath(sceneRef);
            if (!string.IsNullOrEmpty(rootAssetPath))
            {
                if (rootAssetPath == DontDestroyOnLoadScene)
                    guid = DontDestroyOnLoadScene;
                else
                    guid = AssetDatabase.AssetPathToGUID(rootAssetPath);
            }
        }

        private void EnsureInitialized()
		{
			if ( initialized )
				return;

			string assetPath = AssetDatabase.GUIDToAssetPath( guid );
			if(string.IsNullOrEmpty(assetPath) && guid == DontDestroyOnLoadScene)
				assetPath = DontDestroyOnLoadScene;
			
			if ( !AssetDatabase.Exists( assetPath ) 
			  && guid != DontDestroyOnLoadScene)
			{
				this.isMissingReference = true;
				this.errorMessage = " ● Asset Not Found";
			}
			else
			{
				var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( assetPath );
				this.diskFilePath = assetPath;
				if( IsAsset )
				{
					this.virtualFilePath = string.Empty;
					this.asset = asset;
				}
				else if ( IsSceneReference )
				{
					string scenePath;
					UnityEngine.Object sceneRef;
					string expectedPathInScene = string.Empty;
					if( !string.IsNullOrEmpty(cachedPath))
					{
						int idxOfAssetPathInCached = cachedPath.IndexOf(assetPath);
						if(idxOfAssetPathInCached > -1)
						{
							int beginSubPathIdx = idxOfAssetPathInCached + assetPath.Length + 1;
							expectedPathInScene = cachedPath.Substring( beginSubPathIdx );
						}
					}
					var result = AssetDatabase.TryGetSceneRef(
						assetPath, expectedPathInScene, pathCoordinates, 
						out scenePath, out sceneRef);

					if( result == SceneRefResult.Success )
					{
						this.virtualFilePath = scenePath;
						this.asset = sceneRef;
						this.instanceId = AssetDatabase.GetInstanceId(sceneRef);
					}
					else if( result == SceneRefResult.SceneNotLoaded)
					{
						this.isMissingReference = true;
						this.errorMessage = " ● Scene Not Loaded";
					}
					else //if( result == SceneRefResult.GameobjectNotFound)
					{
						this.isMissingReference = true;
						this.errorMessage = " ● Scene Ref Not Found";
					}
				}
				else if ( IsSubAsset )
				{
					var subAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(guid, localId);
					if(subAsset != null)
					{
						this.virtualFilePath = subAsset.name;
						this.asset = subAsset;
					}
					else
					{
						this.errorMessage = string.Format(" ● Subasset '{0}' Not Found", localId);
						this.isMissingReference = true;
					}
				}
			}

			if(this.isMissingReference)
			{
				if( string.IsNullOrEmpty(diskFilePath))
					this.diskFilePath = cachedPath;
				this.virtualFilePath = cachedPath;
				this.fullVirtualAssetPath = cachedPath;
			}
			else if(string.IsNullOrEmpty(this.virtualFilePath))
				this.fullVirtualAssetPath = this.diskFilePath;
			else
				this.fullVirtualAssetPath = string.Concat(this.diskFilePath, "/", this.virtualFilePath);

			this.initialized = true;
		}
    }
}