using FIOSharp.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FIOSharp
{
	public class LocalJsonDataSource : IFixedDataSource
	{
		/// <summary>
		/// The path to the directory that the data source should look for files in, and write them to if file writing is enabled
		/// Be warned, having multiple data sources targetting the same directory can lead to clashing file access errors and break things.
		/// </summary>
		public readonly string DataDirectory;
		/// <summary>
		/// A data source to use if a sutiable file cannot be found. Null if there should be no fallback behaviour
		/// </summary>
		public IFixedDataSource FallbackDataSource;
		/// <summary>
		/// When true, if the fallback is used, a local file should be created with that data.
		/// </summary>
		public bool AutoUpdateOnFallback = true;

		protected const string BUIDLINGS_PATH = "/buildings.json";
		protected const string MATERIALS_PATH = "/materials.json";
		protected const string RECIPES_PATH = "/recipes.json";
		protected const string WORKFORCES_PATH = "/workforces.json";

		//These are thread safe locks to make sure that we don't fight over file access
		protected FlexibleReadWriteLock buildingsLock = new FlexibleReadWriteLock();
		protected FlexibleReadWriteLock materialsLock = new FlexibleReadWriteLock();
		protected FlexibleReadWriteLock recipesLock = new FlexibleReadWriteLock();
		protected FlexibleReadWriteLock workforcesLock = new FlexibleReadWriteLock();


		public LocalJsonDataSource(string dataDirectory, IFixedDataSource fallbackDataSource = null)
		{
			DataDirectory = dataDirectory;
			FallbackDataSource = fallbackDataSource;
			if (Directory.Exists(dataDirectory))
			{
				Directory.CreateDirectory(dataDirectory);
			}
		}

		#region getters
		public List<Building> GetBuildings(List<Material> allMaterials = null)
		{
			if(allMaterials == null)
			{
				allMaterials = GetMaterials();
			}

			return ReadFromFileAndDeserialize(BUIDLINGS_PATH,
				source => source.GetBuildings(allMaterials),
				list => SerializeAndWriteToFile(BUIDLINGS_PATH,
				buildingsLock, list, building => building.ToJson()),
				buildingsLock,
				token => Building.FromJson((JObject)token, allMaterials));
		}

		public async Task<List<Building>> GetBuildingsAsync(List<Material> allMaterials = null)
		{
			Task<List<Material>> materialsTask = allMaterials == null ? GetMaterialsAsync() : Task.FromResult(allMaterials);

			return (await ReadFromFileAndDeserializeAsync(BUIDLINGS_PATH,
				async source => await source.GetBuildingsAsync(await materialsTask),
				list => SerializeAndWriteToFileAsync(BUIDLINGS_PATH, buildingsLock, list, async building => await Task.Run(() => building.ToJson())),
				buildingsLock,
				async token => Building.FromJson((JObject)token, await materialsTask))).ToList();
		}

		public List<Material> GetMaterials()
		{
			return ReadFromFileAndDeserialize(MATERIALS_PATH, source => source.GetMaterials(), list => SerializeAndWriteToFile(MATERIALS_PATH, materialsLock, list), materialsLock);
		}

		public async Task<List<Material>> GetMaterialsAsync()
		{
			return (await ReadFromFileAndDeserializeAsync(MATERIALS_PATH, source => source.GetMaterialsAsync(), list => SerializeAndWriteToFileAsync(MATERIALS_PATH, materialsLock, list), materialsLock)).ToList();
		}

		public List<Recipe> GetRecipes(List<Material> allMaterials = null, List<Building> buildings = null)
		{
			if (allMaterials == null) allMaterials = GetMaterials();
			if (buildings == null) buildings = GetBuildings();
			return ReadFromFileAndDeserialize(RECIPES_PATH,
				source => source.GetRecipes(allMaterials, buildings),
				list => SerializeAndWriteToFile(RECIPES_PATH, recipesLock, list, recipe => recipe.ToJson()),
				recipesLock,
				JToken => Recipe.FromJson((JObject)JToken, allMaterials, buildings));
		}

		public async Task<List<Recipe>> GetRecipesAsync(List<Material> allMaterials = null, List<Building> buildings = null)
		{
			Task<List<Material>> materialsTask = allMaterials == null ? GetMaterialsAsync() : Task.FromResult(allMaterials);
			Task<List<Building>> buildingsTask = buildings == null ? GetBuildingsAsync(await materialsTask) : Task.FromResult(buildings);

			return (await ReadFromFileAndDeserializeAsync(RECIPES_PATH,
				async source => await source.GetRecipesAsync(await materialsTask, await buildingsTask),
				list => SerializeAndWriteToFileAsync(RECIPES_PATH, recipesLock, list, async recipe => await Task.Run(() => recipe.ToJson())),
				recipesLock,
				async token => Recipe.FromJson((JObject)token, await materialsTask, await buildingsTask))).ToList();
		}

		public List<WorkforceRequirement> GetWorkforceRequirements(List<Material> allMaterials = null)
		{
			if (allMaterials == null) allMaterials = GetMaterials();
			return ReadFromFileAndDeserialize(WORKFORCES_PATH,
				source => source.GetWorkforceRequirements(allMaterials),
				list => SerializeAndWriteToFile(WORKFORCES_PATH, workforcesLock, list, requirement => requirement.ToJson()),
				workforcesLock,
				token => WorkforceRequirement.FromJson((JObject)token, allMaterials));
		}

		public async Task<List<WorkforceRequirement>> GetWorkforceRequirementsAsync(List<Material> allMaterials = null)
		{
			Task<List<Material>> materialsTask = allMaterials == null ? GetMaterialsAsync() : Task.FromResult(allMaterials);

			return (await ReadFromFileAndDeserializeAsync(WORKFORCES_PATH,
				async source => await source.GetWorkforceRequirementsAsync(await materialsTask),
				list => SerializeAndWriteToFileAsync(WORKFORCES_PATH, workforcesLock, list, async requirement => await Task.Run(() => requirement.ToJson())),
				workforcesLock,
				async token => WorkforceRequirement.FromJson((JObject)token, await materialsTask))).ToList();
		}
		

		protected List<T> ReadFromFileAndDeserialize<T>(string localPath, Func<IFixedDataSource, List<T>> fallbackFetcher, Action<List<T>> FallbackWriter, FlexibleReadWriteLock fileLock, Func<JToken, T> converter = null)
		{
			if(converter == null)
			{
				converter = token => 
				{
					try { return token.ToObject<T>(); }
					catch (Exception ex) when (ex is JsonSerializationException || ex is FormatException || ex is ArgumentException)
					{
						throw new JsonSchemaException(null, ex);
					}
				};
			}

			if(!File.Exists(DataDirectory + localPath))
			{
				if(FallbackDataSource != null && fallbackFetcher != null)
				{
					List<T> found = fallbackFetcher(FallbackDataSource);
					if (AutoUpdateOnFallback)
					{
						FallbackWriter(found);
					}
					return found;
				}
				throw new LocalFileException(DataDirectory + localPath, new FileNotFoundException());
			}



			try
			{
				return fileLock.RunInRead(() =>
				{
					using (StreamReader file = File.OpenText(DataDirectory + localPath))
					{
						using (JsonTextReader jsonTextReader = new JsonTextReader(file))
						{
							JArray array = (JArray)JToken.ReadFrom(jsonTextReader);
							return array.Select(converter).ToList();
						}
					}
				});
			}
			catch (Exception ex)
			{
				throw new LocalFileException(DataDirectory + localPath, ex);
			}
		}

		protected void SerializeAndWriteToFile<T>(string path, FlexibleReadWriteLock fileLock, List<T> values, Func<T, JToken> converter = null)
		{
			if(converter == null)
			{
				converter = obj => JToken.FromObject(obj);
			}

			JArray jArray = new JArray();
			foreach(JToken token in values.Select(converter))
			{
				jArray.Add(token);
			}

			fileLock.RunInWrite(() =>
			{
				using (StreamWriter file = File.CreateText(DataDirectory + path))
				{
					using (JsonTextWriter jsonTextWriter = new JsonTextWriter(file))
					{
						jsonTextWriter.Formatting = Formatting.Indented;
						jArray.WriteTo(jsonTextWriter);
					}
				}
			});

		}

		protected async Task<IEnumerable<T>> ReadFromFileAndDeserializeAsync<T>(string localPath, Func<IFixedDataSource, Task<List<T>>> fallbackFetcher, Func<List<T>, Task> fallbackWriter, FlexibleReadWriteLock fileLock, Func<JToken, Task<T>> converter = null)
		{
			if (converter == null)
			{
				converter = token => Task.Run(() => 
				{
					try { return token.ToObject<T>(); }
					catch (Exception ex) when (ex is JsonSerializationException || ex is FormatException || ex is ArgumentException)
					{
						throw new JsonSchemaException(null, ex);
					}
				});
			}

			if (!File.Exists(DataDirectory + localPath))
			{
				if (FallbackDataSource != null && fallbackFetcher != null)
				{
					return await getFallbackAsync<T>(localPath, fallbackFetcher, fallbackWriter, fileLock);
				}
				throw new LocalFileException(DataDirectory + localPath, new FileNotFoundException());
			}
			return await Task.WhenAll((await GetJArrayFromFileAsync(localPath, fileLock)).Select(converter));
		}

		protected async Task<IEnumerable<T>> ReadFromFileAndDeserializeAsync<T>(string localPath, Func<IFixedDataSource, Task<List<T>>> fallbackFetcher, Func<List<T>, Task> fallbackWriter, FlexibleReadWriteLock fileLock, Func<JToken, T> converter)
		{
			

			if (!File.Exists(DataDirectory + localPath))
			{
				if (FallbackDataSource != null && fallbackFetcher != null)
				{
					return await getFallbackAsync<T>(localPath, fallbackFetcher, fallbackWriter, fileLock);
				}
				throw new LocalFileException(DataDirectory + localPath, new FileNotFoundException());
			}
			return (await GetJArrayFromFileAsync(localPath, fileLock)).Select(converter);
		}

		protected async Task<JArray> GetJArrayFromFileAsync(string localPath, FlexibleReadWriteLock fileLock)
		{
			try
			{
				return await fileLock.RunInReadAsync(async () => {
					using (StreamReader file = File.OpenText(DataDirectory + localPath))
					{
						using (JsonTextReader jsonTextReader = new JsonTextReader(file))
						{
							return (JArray) await JToken.ReadFromAsync(jsonTextReader);
						}
					}
				});
			}
			catch (Exception ex)
			{
				throw new LocalFileException(DataDirectory + localPath, ex);
			}
		}

		protected async Task<List<T>> getFallbackAsync<T>(string filePath, Func<IFixedDataSource, Task<List<T>>> fallbackProvider, Func<List<T>, Task> fallbackWriter, FlexibleReadWriteLock fileLock)
		{
			if (FallbackDataSource == null) throw new InvalidOperationException("Tried to get a fallback value when no fallback data source was present");
			List<T> found = await fallbackProvider(FallbackDataSource);
			if (AutoUpdateOnFallback)
			{
				await fallbackWriter(found);
				
			}

			return found;
		}

		protected async Task SerializeAndWriteToFileAsync<T>(string path, FlexibleReadWriteLock fileLock, List<T> values, Func<T, JToken> converter)
		{
			JArray jArray = new JArray();
			foreach (JToken token in values.Select(converter))
			{
				jArray.Add(token);
			}

			await fileLock.RunInWriteAsync(async () =>
			{
				using (StreamWriter file = File.CreateText(DataDirectory + path))
				{
					using (JsonTextWriter jsonTextWriter = new JsonTextWriter(file))
					{
						jsonTextWriter.Formatting = Formatting.Indented;
						await jArray.WriteToAsync(jsonTextWriter);
					}
				}
			});
		}

		protected async Task SerializeAndWriteToFileAsync<T>(string path, FlexibleReadWriteLock fileLock, List<T> values, Func<T, Task<JToken>> converter = null)
		{
			if (converter == null)
			{
				converter = obj => Task.Run(() => JToken.FromObject(obj));
			}

			JArray jArray = new JArray();
			foreach (JToken token in await Task.WhenAll(values.Select(converter)))
			{
				jArray.Add(token);
			}

			await fileLock.RunInWriteAsync(async () =>
			{
				using (StreamWriter file = File.CreateText(DataDirectory + path))
				{
					using (JsonTextWriter jsonTextWriter = new JsonTextWriter(file))
					{
						jsonTextWriter.Formatting = Formatting.Indented;
						await jArray.WriteToAsync(jsonTextWriter);
					}
				}
			});
		}
		#endregion

		public void ClearMaterials()
		{
			clearTarget(MATERIALS_PATH, materialsLock);
		}

		public void ClearBuildings()
		{
			clearTarget(BUIDLINGS_PATH, buildingsLock);
		}

		public void ClearRecipes()
		{
			clearTarget(RECIPES_PATH, recipesLock);
		}

		public void ClearWorkforces()
		{
			clearTarget(WORKFORCES_PATH, workforcesLock);
		}

		
		protected void clearTarget(string path, FlexibleReadWriteLock fileLock)
		{
			fileLock.RunInWrite(() => File.Delete(DataDirectory + path));
		}
	}
}
