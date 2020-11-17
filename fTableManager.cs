﻿using Quan_Li_Nha_Hang.DAO;
using Quan_Li_Nha_Hang.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Menu = Quan_Li_Nha_Hang.DTO.Menu;

namespace Quan_Li_Nha_Hang
{
    public partial class fTableManager : Form
    {
        private Account account;

        public Account Account 
        { 
            get => account;
            set 
            { 
                account = value;
                ChangeAccount(account.Chu_Quan);        
            } 
        }

        public fTableManager(Account acc)
        {
            InitializeComponent();
            this.Account = acc;
            LoadTable();
            LoadCategory();
        }

        void ChangeAccount(int type)
        {
            adminToolStripMenuItem.Enabled = type == 1; 
        }

        #region Method
        void LoadCategory()
        {
            List<Category> listCategory = CategoryDAO.Instance.GetListCategory();
            cbLoai_Mon_An.DataSource = listCategory;
            cbLoai_Mon_An.DisplayMember = "Ten_Loai";
        }

        void LoadFoodListCategoryByID(string id)
        {
            List<Food> listFood = FoodDAO.Instance.GetListFoodByCategoryID(id);
            cbThuc_An.DataSource = listFood;
            cbThuc_An.DisplayMember = "Ten_Mon";
        }

        private void LoadTable(int IDBill = 0)
        {
            flpTable.Controls.Clear();
            List<Table> listTable = TableDAO.Instance.LoadTableList();

            foreach (Table item in listTable)
            {
                Button btn = new Button() { Width = TableDAO.btnWidth, Height = TableDAO.btnHeight };
                btn.Text = item.Ban + Environment.NewLine + item.Status;
                btn.Click += Btn_Click;
                btn.Tag = item;
                if (item.Status == "Có Người")
                {
                    btn.BackColor = Color.Aqua;
                }
                else
                {
                    btn.BackColor = Color.Beige;
                }
                flpTable.Controls.Add(btn);                
            }
        }
        void ShowBill(int ID,int IDBill = 0)
        {
            long tongCongTien = 0;
            lsvBill.Items.Clear();
            List<Menu> listBillInfo = MenuDAO.Instance.GetListMenuByID(ID);            
            foreach (Menu item in listBillInfo)
            {
                string TenMonVaLoai = item.TenLoai.ToString()+ " : " + item.FoodName.ToString();
                ListViewItem lsvItem = new ListViewItem(TenMonVaLoai);
                lsvItem.SubItems.Add(item.Count.ToString());
                lsvItem.SubItems.Add(item.DonGia.ToString());
                lsvItem.SubItems.Add(item.TongTien.ToString());
                tongCongTien += item.TongTien;
                lsvBill.Items.Add(lsvItem);
            }
            Total.Text = tongCongTien.ToString("c");
            Total.ForeColor = Color.Red;
            LoadTable();
        }

        #endregion

        #region Events

        private void Btn_Click(object sender, EventArgs e)
        {
            int tableID = ((sender as Button).Tag as Table).Id;
            lsvBill.Tag = (sender as Button).Tag;
            ShowBill(tableID);
        }
        private void đăngXuấtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void thôngTinCáNhânToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fThong_Tin_Ca_Nhan f = new fThong_Tin_Ca_Nhan(account);
            f.UpdateAccount += f_UpdateAccount;
            f.ShowDialog();
        }

        void f_UpdateAccount(object sender,AccountEvent e)
        {
        }

        private void adminToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fAdmin f = new fAdmin();
            f.ShowDialog();
        }

        private void cbLoai_Mon_An_SelectedIndexChanged(object sender, EventArgs e)
        {
            string id;

            ComboBox cb = sender as ComboBox;
            if (cb.SelectedItem == null) return;
            Category select = cb.SelectedItem as Category;

            id = select.ID;

            LoadFoodListCategoryByID(id);
        }

        private void btnThem_Mon_Click(object sender, EventArgs e)
        {
            Table table = lsvBill.Tag as Table; 
            if(table == null){
                MessageBox.Show("Hãy chọn bàn!");
                return;
            }
            int idBIll = BillDAO.Instance.GetUncheckBillIByTableID(table.Id);           
            string MaNhanVien = AccountDAO.Instance.GetMaNhanVien();
            int foodID = (cbThuc_An.SelectedItem as Food).ID_Mon;
            int count = (int)nmFoodCount.Value;
            int checkChiSo = BillInfoDAO.Instance.checkMonCanThayDoiCoExist(foodID, idBIll); //kiểm tra món đó không tồn tại trong bill info thì ko đc numberic < 0
            if (idBIll == -1 && count > 0)
            {
                BillDAO.Instance.InsertBill(table.Id,MaNhanVien);
                BillInfoDAO.Instance.InsertBillInfor(BillDAO.Instance.GetMaxIDBill(), foodID, count, FoodDAO.Instance.GetGiaHienTai(foodID));
            }
            else if(idBIll > 0)
            {
                if((checkChiSo > 0 && count > 0) || (checkChiSo == -1 && count > 0) || (checkChiSo > 0 && count < 0) )
                {
                    BillInfoDAO.Instance.InsertBillInfor(idBIll, foodID, count, FoodDAO.Instance.GetGiaHienTai(foodID));
                }
            }
            ShowBill(table.Id,idBIll);
        }

        private void btnThanh_Toán_Click(object sender, EventArgs e)
        {
            Table ban = lsvBill.Tag as Table;
            if (ban == null)
            {
                MessageBox.Show("Hãy chọn bàn!");
                return;
            }
            Table table = lsvBill.Tag as Table;
            int idBill = BillDAO.Instance.GetUncheckBillIByTableID(table.Id);
            if(idBill != -1)
            {
                if (MessageBox.Show("Bạn có muốn thanh toán hóa đơn cho bàn " + table.Ban, "Thông báo",MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
                {
                    BillDAO.Instance.CheckOut(idBill);
                    ShowBill(table.Id);
                }
            }
            long tongCongTien = 0;
            DataTable Table = DataProvider.Instance1.ExecuteQuery("select * from Thong_Tin_Hoa_Don where ID_Bill = " + idBill);
            if (Table.Rows.Count > 0)
            {
                for (int i = 0; i < Table.Rows.Count; i++)
                {
                    BillInfo billInfo = new BillInfo(Table.Rows[i]);
                    tongCongTien += billInfo.Tong_Tien;
                }
                DataProvider.Instance1.ExecuteNonQuery("update Hoa_Don set Tong_Tien = " + tongCongTien + " where ID_Bill = " + idBill + " and Trang_Thai_Thanh_Toan = 1");
            }
        }

        #endregion
    }
}
