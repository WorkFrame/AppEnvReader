using System;
using System.Windows.Forms;
using NetEti.FileTools;
using NetEti.Globals;
using NetEti.ApplicationEnvironment;

namespace NetEti.DemoApplications
{
    /// <summary>
    /// Demo
    /// </summary>
    public partial class Form1 : Form
    {

        #region public members

        /// <summary>
        /// Ein Beispiel-Wert aus der Kommandozeile
        /// </summary>
        public string CommandLineParameter1 { get; private set; }

        /// <summary>
        /// Ein Beispiel-Wert aus der app.config
        /// </summary>
        public string AppSettingsExample { get; private set; }

        /// <summary>
        /// Ein Beispiel-Wert aus der app.config
        /// </summary>
        public int AppSettingsIntExample { get; private set; }

        /// <summary>
        /// Der Windows-Domain-Name aus dem Environment
        /// </summary>
        public string UserDomainName { get; private set; }

        /// <summary>
        /// Die Programm-Version aus dem Environment
        /// </summary>
        public string ProgramVersion { get; private set; }

        /// <summary>
        /// Ein Beispiel-Wert aus der registry
        /// </summary>
        public string DotNetInstallRoot { get; private set; }

        /// <summary>
        /// Ein Beispiel-Wert aus der test.xml
        /// </summary>
        public string XMLExample { get; private set; }

        /// <summary>
        /// Ein Beispiel-Wert aus der test.ini
        /// </summary>
        public string INIExample { get; private set; }

        /// <summary>
        /// Prioritäten (der erste Treffer gewinnt):
        ///     1. CommandLineAccess - die Kommandozeilen-Argumente 
        ///     2. SettingsAccess    - app.config-Einträge
        ///     3. EnvAccess         - Umgebungsvariablen
        ///     4. RegAccess         - Registry-Einträge
        ///     5. XMLAccess         - eine test.xml
        ///     6. INIAccess         - eine test.Ini
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

        #endregion public members

        #region private members

        // Implementiert IGetStringValue für Zugriffe auf die Kommandozeile.
        private CommandLineAccess _commandLineAccessor;

        // Implementiert IGetStringValue für Zugriffe auf die app.config.
        private SettingsAccess _settingsAccessor { get; set; }

        // Implementiert IGetStringValue für Zugriffe auf das Environment.
        private EnvAccess _envAccessor { get; set; }

        // Implementiert IGetStringValue für Zugriffe auf die Registry.
        private RegAccess _regAccessor { get; set; }

        // Implementiert IGetStringValue für Zugriffe auf XML-Dateien.
        private XmlAccess _xmlAccessor { get; set; }

        // Implementiert IGetStringValue für Zugriffe auf INI-Dateien.
        private IniAccess _iniAccessor { get; set; }

        // Verwaltet die verschiedenen Accessoren.
        private AppEnvReader _appEnvReader;

        // Testpfad in die Registry
        private const string REGDotNetInstallRoot =
            @"SOFTWARE\Wow6432Node\Microsoft\.NETFramework\InstallRoot";

        // Pfad zur Test-XML
        private string _xmlFilePath = @"Test.xml";

        // Pfad zur Test-INI
        private string _iniFilePath = @"Test.ini";

        private void Form1_Load(object sender, EventArgs e)
        {
            this._commandLineAccessor = new CommandLineAccess();
            this._settingsAccessor = new SettingsAccess();
            this._envAccessor = new EnvAccess();
            this._regAccessor = new RegAccess();

            this._appEnvReader = new AppEnvReader();
            //this._appEnvReader = GenericSingletonProvider.GetInstance<AppEnvReader>();

            this._appEnvReader.RegisterStringValueGetter(this._commandLineAccessor);
            this._appEnvReader.RegisterStringValueGetter(this._settingsAccessor);
            this._appEnvReader.RegisterStringValueGetter(this._envAccessor);
            this._appEnvReader.RegisterStringValueGetter(this._regAccessor);
            try
            {
                this._xmlAccessor = new XmlAccess(this._xmlFilePath);
                this._appEnvReader.RegisterStringValueGetter(this._xmlAccessor);
            }
            catch (Exception)
            {
                MessageBox.Show(this._xmlFilePath + " wurde nicht gefunden.");
            }

            try
            {
                this._iniAccessor = new IniAccess(this._iniFilePath);
                this._appEnvReader.RegisterStringValueGetter(this._iniAccessor);
            }
            catch (Exception)
            {
                MessageBox.Show(this._iniFilePath + " wurde nicht gefunden.");
            }

            this.CommandLineParameter1 = this._appEnvReader.GetStringValue("CommandLineParameter1", "nicht gefunden");
            this.AppSettingsExample = this._appEnvReader.GetStringValue("AppSettingsExample", "nicht gefunden");
            try
            {
                this.AppSettingsIntExample = this._appEnvReader.GetValue<int>("AppSettingsExample", -1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "beabsichtigte Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            try
            {
                this.AppSettingsIntExample = this._appEnvReader.GetValue<int>("AppSettingsIntExample", -1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.UserDomainName = this._appEnvReader.GetStringValue("USERDOMAINNAME", "nicht gefunden");
            this.DotNetInstallRoot = this._appEnvReader.GetStringValue(REGDotNetInstallRoot, "nicht gefunden");
            this.XMLExample = this._appEnvReader.GetStringValue("MailTo", "nicht gefunden");
            this.INIExample = this._appEnvReader.GetStringValue("INI_TEST" + Global.SaveColumnDelimiter + "TestKey", "nicht gefunden");
            this.ProgramVersion = this._appEnvReader.GetStringValue("PROGRAMVERSION", "nicht gefunden");

            this.fillListBox();
        }

        private void fillListBox()
        {
            this.listBox1.Items.Add("CommandLineParameter1: " + this.CommandLineParameter1);
            this.listBox1.Items.Add("CommandLineParameter1: " + this.CommandLineParameter1);
            this.listBox1.Items.Add("AppSettingsExample: " + this.AppSettingsExample);
            this.listBox1.Items.Add("AppSettingsIntExample: " + this.AppSettingsIntExample.ToString());
            this.listBox1.Items.Add("Environment-USERDOMAINNAME: " + this.UserDomainName);
            this.listBox1.Items.Add("Registry-DotNetInstallRoot: " + this.DotNetInstallRoot);
            this.listBox1.Items.Add("XML-Example: " + this.XMLExample);
            this.listBox1.Items.Add("INI-Example: " + this.INIExample);
            this.listBox1.Items.Add("ProgramVersion: " + this.ProgramVersion);
        }

        #endregion private members

    }
}
