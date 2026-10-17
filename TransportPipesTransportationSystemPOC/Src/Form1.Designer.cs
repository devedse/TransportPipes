namespace TransportPipesTransportationSystem
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.complexButton = new System.Windows.Forms.Button();
            this.simpleButton = new System.Windows.Forms.Button();
            this.multiResource = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // complexButton
            // 
            this.complexButton.Location = new System.Drawing.Point(92, 220);
            this.complexButton.Name = "complexButton";
            this.complexButton.Size = new System.Drawing.Size(135, 69);
            this.complexButton.TabIndex = 0;
            this.complexButton.Text = "Complex Scenario";
            this.complexButton.UseVisualStyleBackColor = true;
            this.complexButton.Click += new System.EventHandler(this.complexButton_Click);
            // 
            // simpleButton
            // 
            this.simpleButton.Location = new System.Drawing.Point(92, 79);
            this.simpleButton.Name = "simpleButton";
            this.simpleButton.Size = new System.Drawing.Size(135, 65);
            this.simpleButton.TabIndex = 1;
            this.simpleButton.Text = "Simple Scenario";
            this.simpleButton.UseVisualStyleBackColor = true;
            this.simpleButton.Click += new System.EventHandler(this.simpleButton_Click);
            // 
            // multiResource
            // 
            this.multiResource.Location = new System.Drawing.Point(369, 117);
            this.multiResource.Name = "multiResource";
            this.multiResource.Size = new System.Drawing.Size(183, 108);
            this.multiResource.TabIndex = 2;
            this.multiResource.Text = "Multi Resource";
            this.multiResource.UseVisualStyleBackColor = true;
            this.multiResource.Click += new System.EventHandler(this.multiResource_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(795, 413);
            this.Controls.Add(this.multiResource);
            this.Controls.Add(this.simpleButton);
            this.Controls.Add(this.complexButton);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button complexButton;
        private System.Windows.Forms.Button simpleButton;
        private System.Windows.Forms.Button multiResource;
    }
}

