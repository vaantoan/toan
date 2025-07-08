using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Configuration;

namespace WebApplication1
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        string connectionString = ConfigurationManager.ConnectionStrings["QUANLYBHANG"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                LoadSanPham();
        }

        protected void LoadSanPham()
        {
            SqlConnection conn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand("SELECT MaSP, TenSP, Gia, HinhAnh FROM SanPham WHERE TrangThai = 1", conn);
            conn.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            rptSanPham.DataSource = reader;
            rptSanPham.DataBind();
            reader.Close();
            conn.Close();
        }

        protected void btnThem_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string maSP = btn.CommandArgument;
            RepeaterItem item = (RepeaterItem)btn.NamingContainer;
            TextBox txtSL = (TextBox)item.FindControl("txtSoLuong");
            int soLuong = int.Parse(txtSL.Text);

            List<CartItem> gioHang = Session["GioHang"] as List<CartItem> ?? new List<CartItem>();

            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT TenSP, Gia FROM SanPham WHERE MaSP = @MaSP", conn);
            cmd.Parameters.AddWithValue("@MaSP", maSP);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string tenSP = reader["TenSP"].ToString();
                decimal gia = Convert.ToDecimal(reader["Gia"]);

                var itemGH = gioHang.FirstOrDefault(x => x.MaSP == maSP);
                if (itemGH != null)
                    itemGH.SoLuong += soLuong;
                else
                    gioHang.Add(new CartItem { MaSP = maSP, TenSP = tenSP, DonGia = gia, SoLuong = soLuong });

                Session["GioHang"] = gioHang;
                Session["TongTien"] = gioHang.Sum(x => x.SoLuong * x.DonGia);
                lblTongTien.Text = "Tổng thành tiền: " + Session["TongTien"] + " VNĐ";
            }
            reader.Close();
            conn.Close();
        }

        protected void btnDatHang_Click(object sender, EventArgs e)
        {
            string maND = "ND002"; // demo: bạn nên lấy từ tài khoản đăng nhập
            string hoTen = txtHoTen.Text;
            string sdt = txtSDT.Text;
            string diaChi = txtDiaChi.Text;
            decimal tongTien = Convert.ToDecimal(Session["TongTien"] ?? 0);
            string maDH = "DH" + DateTime.Now.Ticks % 100000;

            List<CartItem> gioHang = Session["GioHang"] as List<CartItem>;
            if (gioHang == null || gioHang.Count == 0)
            {
                lblTongTien.Text = "Giỏ hàng trống!";
                return;
            }

            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand cmdDH = new SqlCommand("INSERT INTO DonHang (MaDH, MaND, NgayDat, TrangThai, TongTien) VALUES (@MaDH, @MaND, GETDATE(), N'Chờ xác nhận', @TongTien)", conn);
            cmdDH.Parameters.AddWithValue("@MaDH", maDH);
            cmdDH.Parameters.AddWithValue("@MaND", maND);
            cmdDH.Parameters.AddWithValue("@TongTien", tongTien);
            cmdDH.ExecuteNonQuery();

            foreach (var item in gioHang)
            {
                SqlCommand cmdCT = new SqlCommand("INSERT INTO ChiTietDonHang (MaDH, MaSP, SoLuong, DonGia) VALUES (@MaDH, @MaSP, @SL, @DG)", conn);
                cmdCT.Parameters.AddWithValue("@MaDH", maDH);
                cmdCT.Parameters.AddWithValue("@MaSP", item.MaSP);
                cmdCT.Parameters.AddWithValue("@SL", item.SoLuong);
                cmdCT.Parameters.AddWithValue("@DG", item.DonGia);
                cmdCT.ExecuteNonQuery();
            }

            conn.Close();
            Session["GioHang"] = null;
            Session["TongTien"] = 0;
            lblTongTien.Text = "Đặt hàng thành công!";
        }

        public class CartItem
        {
            public string MaSP { get; set; }
            public string TenSP { get; set; }
            public decimal DonGia { get; set; }
            public int SoLuong { get; set; }
        }
    }
}
