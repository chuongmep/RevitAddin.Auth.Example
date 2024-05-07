namespace RevitAddin.Tasks.Example.Revit.Commands
{
	public static class Globals
	{
		private static string _codeVerifier = "";

		public static string codeVerifier
		{
			get { return _codeVerifier; }
			set { _codeVerifier = value; }
		}

		private static string _accessToken = "";

		public static string AccessToken
		{
			get { return _accessToken; }
			set { _accessToken = value; }
		}

		private static string _refreshToken = "";

		public static string RefreshToken
		{
			get { return _refreshToken; }
			set { _refreshToken = value; }
		}

		private static string _clientId = "";

		public static string ClientId
		{
			get { return _clientId; }
			set { _clientId = value; }
		}

		private static string _callbackUrl = "";

		public static string CallbackURL
		{
			get { return _callbackUrl; }
			set { _callbackUrl = value; }
		}
	}
}
