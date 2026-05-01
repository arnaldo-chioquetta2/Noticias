using System;

namespace NewsImpactRanker.WinForms.Forms
{
    partial class ConfigForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.txtApiKey = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtModel = new System.Windows.Forms.TextBox();
            this.txModeloGrog = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txApiGrog = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtScientificPath = new System.Windows.Forms.TextBox();
            this.btnBrowseScientific = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.txtPromptPath = new System.Windows.Forms.TextBox();
            this.btnBrowsePrompt = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Chave da API Gemini:";
            // 
            // txtApiKey
            // 
            this.txtApiKey.Location = new System.Drawing.Point(12, 31);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(360, 20);
            this.txtApiKey.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Modelo Gemini";
            // 
            // txtModel
            // 
            this.txtModel.Location = new System.Drawing.Point(12, 76);
            this.txtModel.Name = "txtModel";
            this.txtModel.Size = new System.Drawing.Size(360, 20);
            this.txtModel.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 108);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(99, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Chave da API Groq";
            // 
            // txApiGrog
            // 
            this.txApiGrog.Location = new System.Drawing.Point(12, 124);
            this.txApiGrog.Name = "txApiGrog";
            this.txApiGrog.Size = new System.Drawing.Size(360, 20);
            this.txApiGrog.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 153);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Modelo Groq";
            // 
            // txModeloGrog
            // 
            this.txModeloGrog.Location = new System.Drawing.Point(12, 169);
            this.txModeloGrog.Name = "txModeloGrog";
            this.txModeloGrog.Size = new System.Drawing.Size(360, 20);
            this.txModeloGrog.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 198);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(155, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Arquivo de Prompt (Instruções):";
            // 
            // txtPromptPath
            // 
            this.txtPromptPath.Location = new System.Drawing.Point(12, 214);
            this.txtPromptPath.Name = "txtPromptPath";
            this.txtPromptPath.Size = new System.Drawing.Size(278, 20);
            this.txtPromptPath.TabIndex = 9;
            // 
            // btnBrowsePrompt
            // 
            this.btnBrowsePrompt.Location = new System.Drawing.Point(296, 212);
            this.btnBrowsePrompt.Name = "btnBrowsePrompt";
            this.btnBrowsePrompt.Size = new System.Drawing.Size(75, 23);
            this.btnBrowsePrompt.TabIndex = 10;
            this.btnBrowsePrompt.Text = "Procurar...";
            this.btnBrowsePrompt.UseVisualStyleBackColor = true;
            this.btnBrowsePrompt.Click += new System.EventHandler(this.btnBrowsePrompt_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 243);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(143, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Arquivo Notícias Científicas:";
            // 
            // txtScientificPath
            // 
            this.txtScientificPath.Location = new System.Drawing.Point(12, 259);
            this.txtScientificPath.Name = "txtScientificPath";
            this.txtScientificPath.Size = new System.Drawing.Size(278, 20);
            this.txtScientificPath.TabIndex = 12;
            // 
            // btnBrowseScientific
            // 
            this.btnBrowseScientific.Location = new System.Drawing.Point(296, 257);
            this.btnBrowseScientific.Name = "btnBrowseScientific";
            this.btnBrowseScientific.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseScientific.TabIndex = 13;
            this.btnBrowseScientific.Text = "Procurar...";
            this.btnBrowseScientific.UseVisualStyleBackColor = true;
            this.btnBrowseScientific.Click += new System.EventHandler(this.btnBrowseScientific_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(12, 305);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 14;
            this.btnSave.Text = "Salvar";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(296, 305);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ConfigForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(392, 345);
            this.Controls.Add(this.btnBrowseScientific);
            this.Controls.Add(this.txtScientificPath);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btnBrowsePrompt);
            this.Controls.Add(this.txtPromptPath);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txModeloGrog);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txApiGrog);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtModel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtApiKey);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configurações";
            this.Load += new System.EventHandler(this.ConfigForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtApiKey;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtModel;
        private System.Windows.Forms.TextBox txModeloGrog;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txApiGrog;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtScientificPath;
        private System.Windows.Forms.Button btnBrowseScientific;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtPromptPath;
        private System.Windows.Forms.Button btnBrowsePrompt;
    }
}
