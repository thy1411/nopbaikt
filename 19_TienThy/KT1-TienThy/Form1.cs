using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace BT02
{
    public partial class Form1 : Form
    {
        string strcon = @"server=.;database=QLSV_TyTy; integrated security = true";
        DataSet ds = new DataSet();
        //Khai báo đối tượng DataAdapter để sử dụng cho các bảng dữ liệu
        SqlDataAdapter adpSinhVien, adpKhoa, adpKetQua;
        //Khai báo đối tượng CommandBuilder SinhVien để cập nhật dữ liệu cho bảng SinhVien
        SqlCommandBuilder cmbSinhVien;
        BindingSource bs = new BindingSource();
        int stt = 0;
        public Form1()
        {
            InitializeComponent();
            bs.CurrentChanged += Bs_CurrentChanged;
        }

        private void Bs_CurrentChanged(object sender, EventArgs e)
        {
            lblSTT.Text = bs.Position + 1 + "/" + bs.Count;
            txtTongDiem.Text = TongDiem(txtMaSV.Text).ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //1. Khởi tạo các đối tượng
            Khoi_Tao_Cac_Doi_Tuong();
            //2. Tạo cấu trúc, đọc dữ liệu từ các bảng trong CSDL vào DataTable
            Doc_Du_Lieu();
            Moc_Noi_Quan_He();
            //3. Lớp BindingSource là lớp trung gian: Đối tượng chứa dữ liệu và các điều khiển
            //Có các phương thức: DataSource (là DataSet), DataMember (tên bảng trong DataSet)
            Khoi_Tao_BindingSource();
            //4. Khởi tạo Combobox Khoa
            Khoi_Tao_CboMaKH();
            //5. Liên kết các điều khiển trên Form với BindingSource bs
            Lien_Ket_Dieu_Khien();
            //Liên kết Control BìningNavigator
            bdnSinhVien.BindingSource = bs;
        }
        private void Doc_Du_Lieu()
        {
            //Sao chép cấu trúc và đưa dữ liệu từ CSD vào DataTable
            adpKhoa.FillSchema(ds, SchemaType.Source, "KHOA");
            adpKhoa.Fill(ds, "KHOA");

            adpSinhVien.FillSchema(ds, SchemaType.Source, "SINHVIEN");
            adpSinhVien.Fill(ds, "SINHVIEN");

            adpKetQua.FillSchema(ds, SchemaType.Source, "KETQUA");
            adpKetQua.Fill(ds, "KETQUA");
        }

        private void Khoi_Tao_Cac_Doi_Tuong()
        {
            //1. Khởi tạo các đối tượng DataAdapter
            adpKhoa = new SqlDataAdapter("Select * from KHOA", strcon);
            adpSinhVien = new SqlDataAdapter("Select * from SINHVIEN", strcon);
            adpKetQua = new SqlDataAdapter("Select * from KETQUA", strcon);

            //2. Khởi tạo đối tượng CommanBuilder
            cmbSinhVien = new SqlCommandBuilder(adpSinhVien);
        }
        private double TongDiem(string MSV)
        {
            double kq = 0;
            Object td = ds.Tables["KETQUA"].Compute("sum(Diem)", "MaSV='" + MSV + "'");
            //Lưu ý: Trường hợp SV không có điểm thì phương thức compute trả về giá trị DBNull
            if (td == DBNull.Value)
                kq = 0;
            else
                kq = Convert.ToDouble(td);
            return kq;
        }
        private void Lien_Ket_Dieu_Khien()
        {
            //Chú ý các điều khiển dữ liệu và tính toán
            foreach (Control ctl in this.Controls)
                if (ctl is TextBox && ctl.Name != "txtTongDiem" && ctl.Name != "txtPhai")
                    ctl.DataBindings.Add("text", bs, ctl.Name.Substring(3), true);
                else if (ctl is ComboBox)
                    ctl.DataBindings.Add("Selectedvalue", bs, ctl.Name.Substring(3), true);
                else if (ctl is DateTimePicker)
                    ctl.DataBindings.Add("value", bs, ctl.Name.Substring(3), true);
            //Binding cho điều khiển Phái
            Binding bdphai = new Binding("text", bs, "Phai", true);
            //Sử dụng các phương thức: khi hiển thị và nhận lại giá trị
            bdphai.Format += Bdphai_Format;
            bdphai.Parse += Bdphai_Parse;
            txtPhai.DataBindings.Add(bdphai);
        }

        private void Bdphai_Parse(object sender, ConvertEventArgs e)
        {
            if (e.Value == null) return;
            e.Value = e.Value.ToString().ToUpper() == "Nam" ? true : false;
        }

        private void Bdphai_Format(object sender, ConvertEventArgs e)
        {
            if (e.Value == DBNull.Value || e.Value == null) return;
            e.Value = (Boolean)e.Value ? "Nam" : "Nữ";
        }

        private void Khoi_Tao_CboMaKH()
        {
            cboMaKH.DisplayMember = "TenKH";
            cboMaKH.ValueMember = "MaKH";
            cboMaKH.DataSource = ds.Tables["KHOA"];
        }

        private void btnTruoc_Click(object sender, EventArgs e)
        {
            bs.MovePrevious();
        }

        private void btnSau_Click(object sender, EventArgs e)
        {
            bs.MoveNext();
        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            txtMaSV.ReadOnly = false;
            stt = bs.Position;
            //Thêm mới
            bs.AddNew();
            cboMaKH.SelectedIndex = 0;
            dtpNgaySinh.Value = new DateTime(2006, 1, 1);
            txtMaSV.Focus();
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            //Xác định dòng cần huỷ => Sử dụng hàm Find
            DataRow rsv = (bs.Current as DataRowView).Row;
            //Cần kiểm tra: Nếu rsv tồn tại những dòng liên quan trong tblKetQua => Không cho xoá. Ngược lại thì cho xoá
            //sử dụng hàm GetChilRow để kiểm tra những dòng liên quan có tồn tại hay không? Giá trị trả về của hàm này là 1 mảng
            DataRow[] Mang_Dong_Lien_Quan = rsv.GetChildRows("FK_SV_KQ");
            if (Mang_Dong_Lien_Quan.Length > 0) //Có tôn tại những dòng liên quan trong tblKetQua
                MessageBox.Show("Không xoá SV được vì đã có kết quả thi");
            else
            {
                DialogResult tl;
                tl = MessageBox.Show("Xoá sinh viên này không?", "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (tl == DialogResult.Yes)
                {
                    //Xoá trong DataTable
                    bs.RemoveCurrent();
                    //Xoá trong CSDL
                    int n = adpSinhVien.Update(ds, "SINHVIEN");
                    if (n > 0)
                        MessageBox.Show("Xoá sinh viên thành công");
                }
            }
        }

        private void btnGhi_Click(object sender, EventArgs e)
        {
            if (txtMaSV.ReadOnly == false)//Ghi khi thêm mới
            {
                //Kiểm tra MaSV bị trùng(khoá chính
                DataRow r = ds.Tables["SINHVIEN"].Rows.Find(txtMaSV.Text);
                if (r != null)
                {
                    MessageBox.Show("Mã sinh viên bị trùng, vui lòng nhập lại!");
                    txtMaSV.Focus();
                    return;
                }
            }
            //Cập nhật lại việc thêm mới hay sửa trong DataTable
            bs.EndEdit();
            //Cập nhật vào CSDL
            int n = adpSinhVien.Update(ds, "SINHVIEN");
            if (n > 0)
                MessageBox.Show("Cập nhật (THÊM/SỬA) sinh viên thành công!");
            txtMaSV.ReadOnly = true;
        }

        private void btnKhong_Click(object sender, EventArgs e)
        {
            //Xử dụng phương thức CancelEdit() để huỷ bỏ sự thay đổi trên BindingSource
            bs.CancelEdit();
            txtMaSV.ReadOnly = true;
            bs.Position = stt;
        }

        private void Khoi_Tao_BindingSource()
        {
            bs.DataSource = ds;
            bs.DataMember = "SINHVIEN";
        }

        private void Moc_Noi_Quan_He()
        {
            //Tạo quan hệ giữa tblKhoa và tblSinhVien
            ds.Relations.Add("FK_KH_SV", ds.Tables["KHOA"].Columns["MaKH"], ds.Tables["SINHVIEN"].Columns["MaKH"], true);
            //Tạo quan hệ giữa tblSinhVien và tblKetQua
            ds.Relations.Add("FK_SV_KQ", ds.Tables["SINHVIEN"].Columns["MaSV"], ds.Tables["KETQUA"].Columns["MaSV"], true);
            //Loại bỏ cacase Delete trong các quan hệ
            ds.Relations["FK_KH_SV"].ChildKeyConstraint.DeleteRule = Rule.None;
            ds.Relations["FK_SV_KQ"].ChildKeyConstraint.DeleteRule = Rule.None;
        }
    }
}
