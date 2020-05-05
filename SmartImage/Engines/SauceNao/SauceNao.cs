#region

using System;
using System.IO;
using System.Json;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Xml;
using Neocmd;
using OpenQA.Selenium;
using RapidSelenium;
using RestSharp;
using SmartImage.Model;
using SmartImage.Utilities;
using JsonObject = System.Json.JsonObject;

#endregion

// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace SmartImage.Engines.SauceNao
{
	// https://github.com/RoxasShadow/SauceNao-Windows
	// https://github.com/LazDisco/SharpNao

	public sealed class SauceNao : BaseSauceNao
	{
		private const string ENDPOINT = BASE_URL + "search.php";

		private const string ACC_OV_URL  = BASE_URL + "user.php?page=account-overview";
		private const string ACC_API_URL = BASE_URL + "user.php?page=search-api";

		// todo: rename all

		private static readonly By ByUsername =
			By.CssSelector("body > form:nth-child(7) > input[type=text]:nth-child(1)");

		private static readonly By ByEmail = By.CssSelector("body > form:nth-child(7) > input[type=text]:nth-child(3)");

		private static readonly By ByPassword =
			By.CssSelector("body > form:nth-child(7) > input[type=password]:nth-child(5)");

		private static readonly By ByPassword2 =
			By.CssSelector("body > form:nth-child(7) > input[type=password]:nth-child(7)");

		private static readonly By ByRegister =
			By.CssSelector("body > form:nth-child(7) > input[type=submit]:nth-child(10)");

		private static readonly By ByBody = By.CssSelector("body");

		private static readonly By ByApiKey = By.CssSelector("#middle > form");

		private readonly string m_apiKey;

		private readonly RestClient m_client;

		private SauceNao(string apiKey)
		{
			m_client = new RestClient(ENDPOINT);
			m_apiKey = apiKey;
		}

		public SauceNao() : this(Config.SauceNaoAuth.Id) { }

		private SauceNaoResult[] GetApiResults(string url)
		{
			if (m_apiKey == null) {
				// todo
				return Array.Empty<SauceNaoResult>();
			}

			var req = new RestRequest();
			req.AddQueryParameter("db", "999");
			req.AddQueryParameter("output_type", "2");
			req.AddQueryParameter("numres", "16");
			req.AddQueryParameter("api_key", m_apiKey);
			req.AddQueryParameter("url", url);


			var res = m_client.Execute(req);

			WebAgent.AssertResponse(res);


			//Console.WriteLine("{0} {1} {2}", res.IsSuccessful, res.ResponseStatus, res.StatusCode);
			//Console.WriteLine(res.Content);


			string c = res.Content;


			if (String.IsNullOrWhiteSpace(c)) {
				CliOutput.WriteError("No SN results!");
			}

			return ReadResults(c);
		}

		private static SauceNaoResult[] ReadResults(string c)
		{
			// From https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs

			var jsonString = JsonValue.Parse(c);

			if (jsonString is JsonObject jsonObject) {
				var jsonArray = jsonObject["results"];
				for (int i = 0; i < jsonArray.Count; i++) {
					var    header = jsonArray[i]["header"];
					var    data   = jsonArray[i]["data"];
					string obj    = header.ToString();
					obj          =  obj.Remove(obj.Length - 1);
					obj          += data.ToString().Remove(0, 1).Insert(0, ",");
					jsonArray[i] =  JsonValue.Parse(obj);
				}

				string json = jsonArray.ToString();
				json = json.Insert(json.Length - 1, "}").Insert(0, "{\"results\":");
				using var stream =
					JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json),
					                                         XmlDictionaryReaderQuotas.Max);
				var serializer = new DataContractJsonSerializer(typeof(SauceNaoResponse));
				var result     = serializer.ReadObject(stream) as SauceNaoResponse;
				stream.Dispose();
				if (result is null)
					return null;

				foreach (var t in result.Results) {
					t.WebsiteTitle = Strings.SplitPascalCase(t.Index.ToString());
				}

				return result.Results;
			}

			return null;
		}

		private static SauceNaoResult GetBestApiResult(SauceNaoResult[] results)
		{
			var sauceNao = results.OrderByDescending(r => r.Similarity).First();

			return sauceNao;
		}


		public override SearchResult GetResult(string url)
		{
			SauceNaoResult[] sn = GetApiResults(url);

			if (sn == null) {
				return new SearchResult(null, Name);
			}

			var best = GetBestApiResult(sn);


			if (best != null) {
				string bestUrl = best?.Url?[0];

				var sr = new SearchResult(bestUrl, Name, best.Similarity / 100);
				sr.ExtendedInfo.Add("API configured");
				return sr;
			}

			return new SearchResult(null, Name);
		}

		public static GenericAccount CreateAccount(bool auto)
		{
			RapidWebDriver rwd;


			try {
				CliOutput.WriteInfo("Please wait...");
				rwd = RapidWebDriver.CreateQuick(true);
			}
			catch (Exception exception) {
				CliOutput.WriteError("Exception: {0}", exception.Message);
				//throw;
				CliOutput.WriteError("Error creating webdriver");

				return GenericAccount.Null;
			}

			string uname, pwd, email;

			if (auto) {
				uname = Strings.RandomString(10);
				pwd   = Strings.RandomString(10);
				email = Agent.EMail.GetEmail();
			}
			else {
				Console.Write("Username: ");
				uname = Console.ReadLine();

				Console.Write("Password: ");
				pwd = Console.ReadLine();

				Console.Write("Email: ");
				email = Console.ReadLine();
			}

			Console.WriteLine("\nUsername: {0}\nPassword: {1}\nEmail: {2}\n", uname, pwd, email);


			try {
				CliOutput.WriteInfo("Registering account...");
				var acc = SauceNao.CreateAccountInternal(rwd, uname, email, pwd);

				CliOutput.WriteInfo("Account information:");

				var accStr = acc.ToString();
				var output = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) 
				             + "\\saucenao_account.txt";
				
				File.WriteAllText(output, accStr);

				Console.WriteLine(accStr);

				CliOutput.WriteInfo("Cleaning up...");
				rwd.Dispose();

				return acc;
			}
			catch (Exception exception) {
				CliOutput.WriteError("Exception: {0}", exception.Message);
				//throw;
				CliOutput.WriteError("Error creating account");

				return GenericAccount.Null;
			}
		}

		private static GenericAccount CreateAccountInternal(RapidWebDriver rwd,
		                                                    string         username = null,
		                                                    string         email    = null,
		                                                    string         password = null)
		{
			var cd = rwd.Value;

			cd.Url = BASE_URL + "user.php";

			var usernameEle = cd.FindElement(ByUsername);
			usernameEle.SendKeys(username);


			var emailEle = cd.FindElement(ByEmail);
			emailEle.SendKeys(email);


			var pwdEle = cd.FindElement(ByPassword);
			pwdEle.SendKeys(password);


			var pwd2Ele = cd.FindElement(ByPassword2);
			pwd2Ele.SendKeys(password);

			var regEle = cd.FindElement(ByRegister);
			regEle.Click();

			var body     = cd.FindElement(ByBody);
			var response = body.Text;

			Thread.Sleep(TimeSpan.FromSeconds(5));

			if (cd.Url != ACC_OV_URL || !response.Contains("welcome")) {
				CliOutput.WriteError("Error registering: {0} (body: {1})", cd.Url, response);
				return GenericAccount.Null;
			}

			CliOutput.WriteSuccess("Success!");

			// https://saucenao.com/user.php?page=search-api

			cd.Url = ACC_API_URL;

			var apiEle  = cd.FindElement(ByApiKey);
			var apiText = apiEle.Text.Split(' ')[2];

			return new GenericAccount(username, password, email, apiText);
		}
	}
}