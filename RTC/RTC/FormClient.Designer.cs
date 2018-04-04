﻿namespace RTC {
    partial class FormClient {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.lblID = new System.Windows.Forms.Label();
            this.txtID = new System.Windows.Forms.TextBox();
            this.lblServerIP = new System.Windows.Forms.Label();
            this.txtServerIP = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.txtClientLog = new System.Windows.Forms.TextBox();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblID
            // 
            this.lblID.AutoSize = true;
            this.lblID.Location = new System.Drawing.Point(26, 29);
            this.lblID.Name = "lblID";
            this.lblID.Size = new System.Drawing.Size(41, 12);
            this.lblID.TabIndex = 0;
            this.lblID.Text = "아이디";
            // 
            // txtID
            // 
            this.txtID.Location = new System.Drawing.Point(70, 24);
            this.txtID.Name = "txtID";
            this.txtID.Size = new System.Drawing.Size(100, 21);
            this.txtID.TabIndex = 1;
            // 
            // lblServerIP
            // 
            this.lblServerIP.AutoSize = true;
            this.lblServerIP.Location = new System.Drawing.Point(207, 29);
            this.lblServerIP.Name = "lblServerIP";
            this.lblServerIP.Size = new System.Drawing.Size(57, 12);
            this.lblServerIP.TabIndex = 0;
            this.lblServerIP.Text = "서버 주소";
            // 
            // txtServerIP
            // 
            this.txtServerIP.Location = new System.Drawing.Point(270, 24);
            this.txtServerIP.MaxLength = 15;
            this.txtServerIP.Name = "txtServerIP";
            this.txtServerIP.Size = new System.Drawing.Size(147, 21);
            this.txtServerIP.TabIndex = 2;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(469, 24);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(113, 23);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Log-In";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // txtClientLog
            // 
            this.txtClientLog.Location = new System.Drawing.Point(28, 65);
            this.txtClientLog.Multiline = true;
            this.txtClientLog.Name = "txtClientLog";
            this.txtClientLog.Size = new System.Drawing.Size(554, 220);
            this.txtClientLog.TabIndex = 4;
            this.txtClientLog.TabStop = false;
            // 
            // txtMessage
            // 
            this.txtMessage.Location = new System.Drawing.Point(28, 291);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(554, 21);
            this.txtMessage.TabIndex = 4;
            this.txtMessage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMessage_KeyPress);
            // 
            // FormClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(617, 329);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.txtClientLog);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.txtServerIP);
            this.Controls.Add(this.lblServerIP);
            this.Controls.Add(this.txtID);
            this.Controls.Add(this.lblID);
            this.Name = "FormClient";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RTC Client";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormClient_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblID;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.Label lblServerIP;
        private System.Windows.Forms.TextBox txtServerIP;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.TextBox txtClientLog;
        private System.Windows.Forms.TextBox txtMessage;
    }
}