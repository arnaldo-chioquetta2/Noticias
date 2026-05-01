    using System;
using System.Windows.Forms;
using NewsImpactRanker.WinForms.Models;
using NewsImpactRanker.WinForms.Storage;

namespace NewsImpactRanker.WinForms.Forms
{
    public partial class ConfigForm : Form
    {
        public ConfigForm()
        {
            InitializeComponent();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            AppConfig config = StorageManager.LoadConfig();

            if (config != null)
            {
                // Campos do Gemini
                txtApiKey.Text = config.GeminiApiKey;
                txtModel.Text = config.Model;

                // Campos do Groq que você adicionou
                txApiGrog.Text = config.GroqApiKey;
                txModeloGrog.Text = config.GroqModel;
                txtPromptPath.Text = config.PromptFilePath;
                txtScientificPath.Text = config.ScientificNewsFilePath;
            }
        }        

        private void btnSave_Click(object sender, EventArgs e)
        {
            AppConfig updatedConfig = new AppConfig
            {
                // Salva Gemini
                GeminiApiKey = txtApiKey.Text.Trim(),
                Model = txtModel.Text.Trim(),

                // Salva Groq
                GroqApiKey = txApiGrog.Text.Trim(),
                GroqModel = txModeloGrog.Text.Trim()
            };

            try
            {
                updatedConfig.ScientificNewsFilePath = txtScientificPath.Text;
                updatedConfig.PromptFilePath = txtPromptPath.Text;

                updatedConfig.ScientificNewsFilePath = txtScientificPath.Text.Trim();

                StorageManager.SaveConfig(updatedConfig);
                MessageBox.Show("Configurações das IAs salvas com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configurações: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnBrowseScientific_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Definir Arquivo de Notícias Científicas";
                sfd.Filter = "Arquivos de Texto (*.txt)|*.txt";
                sfd.DefaultExt = "txt";

                // Desativa a mensagem chata perguntando se deseja substituir o arquivo
                sfd.OverwritePrompt = false;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    txtScientificPath.Text = sfd.FileName;
                }
            }
        }

        private void btnBrowsePrompt_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Selecionar Ficheiro de Prompt";
                ofd.Filter = "Ficheiros de Texto (*.txt)|*.txt|Todos os ficheiros (*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtPromptPath.Text = ofd.FileName;
                }
            }
        }

    }

}
