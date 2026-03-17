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
            }
        }

        //private void ConfigForm_Load(object sender, EventArgs e)
        //{
        //    var config = StorageManager.LoadConfig();

        //    txtApiKey.Text = config.AiApiKey;
        //    txtModel.Text = config.AiModel ?? "mixtral-8x7b-32768";
        //    txtNewsFile.Text = config.NewsFilePath ?? "";
        //}

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
    }

    //    private void btnBrowse_Click(object sender, EventArgs e)
    //    {
    //        using (var dialog = new OpenFileDialog())
    //        {
    //            dialog.Filter = "Arquivos de texto (*.txt)|*.txt";
    //            dialog.Title = "Selecione o arquivo com as URLs";

    //            if (dialog.ShowDialog() == DialogResult.OK)
    //            {
    //                txtNewsFile.Text = dialog.FileName;
    //            }
    //        }
    //    }

    //}
}
