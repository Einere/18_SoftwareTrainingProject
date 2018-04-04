using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RTC {
    public partial class FormMain :Form {
        public FormMain() {
            InitializeComponent();
        }

        private void btnServer_Click(object sender, EventArgs e) {
            ShowForm(new FormServer());
        }

        private void btnClient_Click(object sender, EventArgs e) {
            ShowForm(new FormClient());
        }

        private void ShowForm(Form f) {
            this.Hide();                                // 현재 폼을 숨김
            f.FormClosed += (s, arg) => this.Close();   // 새로운 폼 종료 이벤트 추가
            f.Show();                                   // 새로운 폼 보여주기
        }
    }
}
