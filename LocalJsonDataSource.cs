using FIOSharp.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
		protected const string MATERIALS_PATH = "/buildings.json";
		protected const string RECIPES_PATH = "/recipes.json";
		protected const string WORKFORCES_PATH = "/workforces.json";

		protected ReaderWriterLockSlim buildingsLock = new ReaderWriterLockSlim();
		protected ReaderWriterLockSlim materialsLock = new ReaderWriterLockSlim();
		protected ReaderWriterLockSlim recipesLock = new ReaderWriterLockSlim();
		protected ReaderWriterLockSlim workforcesLoc = new ReaderWriterLockSlim();


		public LocalJsonDataSource(string dataDirectory, IFixedDataSource fallbackDataSource = null)
		{
			DataDirectory = dataDirectory;
			FallbackDataSource = fallbackDataSource;
			if (Directory.Exists(dataDirectory))
			{
				Directory.CreateDirectory(dataDirectory);
			}
		}

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
				async token => await Task.Run(() => Building.FromJson((JObject)token, allMaterials)))).ToList();
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
			throw new NotImplementedException();
		}

		public Task<List<Recipe>> GetRecipesAsync(List<Material> allMaterials = null, List<Building> buildings = null)
		{
			throw new NotImplementedException();
		}

		public List<WorkforceRequirement> GetWorkforceRequirements(List<Material> allMaterials = null)
		{
			throw new NotImplementedException();
		}

		public Task<List<WorkforceRequirement>> GetWorkforceRequirementsAsync(List<Material> allMaterials = null)
		{
			throw new NotImplementedException();
		}
		

		protected List<T> ReadFromFileAndDeserialize<T>(string localPath, Func<IFixedDataSource, List<T>> fallbackFetcher, Action<List<T>> FallbackWriter, ReaderWriterLockSlim fileLock, Func<JToken, T> converter = null)
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
				fileLock.EnterReadLock();
				using (StreamReader file = File.OpenText(DataDirectory + localPath))
				{
					using (JsonTextReader jsonTextReader = new JsonTextReader(file))
					{
						JArray array = (JArray)JToken.ReadFrom(jsonTextReader);
						return array.Select(converter).ToList();
					}
				}
			}
			catch (Exception ex)
			{
				throw new LocalFileException(DataDirectory + localPath, ex);
			}
			finally
			{
				fileLock.ExitReadLock();
			}
		}

		protected void SerializeAndWriteToFile<T>(string path, ReaderWriterLockSlim fileLock, List<T> values, Func<T, JToken> converter = null)
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

			try
			{
				fileLock.EnterWriteLock();
				using (StreamWriter file = File.CreateText(path))
				{
					using (JsonTextWriter jsonTextWriter = new JsonTextWriter(file))
					{
						jArray.WriteTo(jsonTextWriter);
					}
				}
			}
			finally
			{
				fileLock.ExitWriteLock();
			} 
		}

		protected async Task<IEnumerable<T>> ReadFromFileAndDeserializeAsync<T>(string localPath, Func<IFixedDataSource, Task<List<T>>> fallbackFetcher, Func<List<T>, Task> fallbackWriter, ReaderWriterLockSlim fileLock, Func<JToken, Task<T>> converter = null)
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

		protected async Task<IEnumerable<T>> ReadFromFileAndDeserializeAsync<T>(string localPath, Func<IFixedDataSource, Task<List<T>>> fallbackFetcher, Func<List<T>, Task> fallbackWriter, ReaderWriterLockSlim fileLock, Func<JToken, T> converter)
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

		protected async Task<JArray> GetJArrayFromFileAsync(string localPath, ReaderWriterLockSlim fileLock)
		{
			try
			{
				fileLock.EnterReadLock();
				using (StreamReader file = File.OpenText(DataDirectory + localPath))
				{
					using (JsonTextReader jsonTextReader = new JsonTextReader(file))
					{
						return (JArray) await JToken.ReadFromAsync(jsonTextReader);
					}
				}
			}
			catch (Exception ex)
			{
				throw new LocalFileException(DataDirectory + localPath, ex);
			}
			finally
			{
				fileLock.ExitReadLock();
			}
		}

		protected async Task<List<T>> getFallbackAsync<T>(string filePath, Func<IFixedDataSource, Task<List<T>>> fallbackProvider, Func<List<T>, Task> fallbackWriter, ReaderWriterLockSlim fileLock)
		{
			if (FallbackDataSource == null) throw new InvalidOperationException("Tried to get a fallback value when no fallback data source was present");
			List<T> found = await fallbackProvider(FallbackDataSource);
			if (AutoUpdateOnFallback)
			{
				try
				{
					fileLock.EnterWriteLock();
					await fallbackWriter(found);
				}
				finally
				{
					fileLock.ExitWriteLock();
				}
				
			}

			return found;
		}

		protected async Task SerializeAndWriteToFileAsync<T>(string path, ReaderWriterLockSlim fileLock, List<T> values, Func<T, JToken> converter)
		{
			JArray jArray = new JArray();
			foreach (JToken token in values.Select(converter))
			{
				jArray.Add(token);
			}

			try
			{
				fileLock.EnterWriteLock();
				using (StreamWriter file = File.CreateText(path))
				{
					using (JsonTextWriter jsonTextWriter = new JsonTextWriter(file))
					{
						await jArray.WriteToAsync(jsonTextWriter);
					}
				}
			}
			finally
			{
				fileLock.ExitWriteLock();
			}
		}

		protected async Task SerializeAndWriteToFileAsync<T>(string path, ReaderWriterLockSlim fileLock, List<T> values, Func<T, Task<JToken>> converter = null)
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

			try
			{
				fileLock.EnterWriteLock();
				using (StreamWriter file = File.CreateText(path))
				{
					using (JsonTextWriter jsonTextWriter = new JsonTextWriter(file))
					{
						await jArray.WriteToAsync(jsonTextWriter);
					}
				}
			}
			finally
			{
				fileLock.ExitWriteLock();
			}
		}
	}
}
