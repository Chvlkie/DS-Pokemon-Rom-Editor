using DSPRE.Editors.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DSPRE.RomInfo;

namespace DSPRE.Editors
{
    public partial class FlyEditor : Form
    {
        private const uint DiamondPearlOffset = 0xF2224;
        private const int DiamondPearlTableSize = 20;
        private const uint HeartGoldSoulSilverOffset = 0xF9E82;
        private const int HeartGoldSoulSilverTableSize = 30;
        private const uint PlatinumOffset = 0xE97B4;
        private const int PlatinumTableSize = 20;
        private static GameFamilies GameFamily;
        private List<string> Headers;
        private bool isFormClosing = false;
        private bool isValidInput = true;

        private List<FlyTableRowDpPlat> TableDataDpPlat;
        private List<FlyTableRowHgss> TableDataHgss;

        public FlyEditor(GameFamilies gameFamily, List<string> headers)
        {
            this.FormClosing += FlyEditor_FormClosing;
            GameFamily = gameFamily;
            Headers = headers;
            TableDataHgss = new List<FlyTableRowHgss>();
            TableDataDpPlat = new List<FlyTableRowDpPlat>();
            InitializeComponent();
            PopulateColumns();
            BeginPopulateFlyTableData();
        }

        private static uint FlyTableOffset
        {
            get
            {
                switch (GameFamily)
                {
                    case GameFamilies.DP:
                        return DiamondPearlOffset;

                    case GameFamilies.Plat:
                        return PlatinumOffset;

                    case GameFamilies.HGSS:
                        return HeartGoldSoulSilverOffset;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(GameFamily), "Unknown game family");
                }
            }
        }

        private static int TableSize
        {
            get
            {
                switch (GameFamily)
                {
                    case GameFamilies.DP:
                        return DiamondPearlTableSize;

                    case GameFamilies.Plat:
                        return PlatinumTableSize;

                    case GameFamilies.HGSS:
                        return HeartGoldSoulSilverTableSize;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(GameFamily), "Unknown game family");
                }
            }
        }

        public async void BeginPopulateFlyTableData()
        {
            await PopulateFlyTableDataAsync();
            if (GameFamily == GameFamilies.DP || GameFamily == GameFamilies.Plat)
            {
                await PopulateTablesFromDataDpPlatAsync();
            }
            else if (GameFamily == GameFamilies.HGSS)
            {
                await PopulateTablesFromDataHgssAsync();
            }
        }

        public async Task PopulateFlyTableDataAsync()
        {
            TableDataHgss?.Clear();
            TableDataDpPlat?.Clear();

            try
            {
                using (FileStream fs = new FileStream(RomInfo.arm9Path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        fs.Seek(FlyTableOffset, SeekOrigin.Begin);

                        for (int i = 0; i < TableSize; i++)
                        {
                            if (GameFamily == GameFamilies.HGSS)
                            {
                                FlyTableRowHgss row = new FlyTableRowHgss
                                {
                                    HeaderIdGameOver = await ReadUInt16Async(reader),
                                    LocalX = await ReadByteAsync(reader),
                                    LocalY = await ReadByteAsync(reader),
                                    HeaderIdFly = await ReadUInt16Async(reader),
                                    GlobalX = await ReadUInt16Async(reader),
                                    GlobalY = await ReadUInt16Async(reader),
                                    HeaderIdUnlockWarp = await ReadUInt16Async(reader),
                                    GlobalXUnlock = await ReadUInt16Async(reader),
                                    GlobalYUnlock = await ReadUInt16Async(reader),
                                    UnlockId = await ReadByteAsync(reader),
                                    WarpCondition = await ReadByteAsync(reader)
                                };
                                TableDataHgss.Add(row);
                            }
                            else if (GameFamily == GameFamilies.DP || GameFamily == GameFamilies.Plat)
                            {
                                FlyTableRowDpPlat row = new FlyTableRowDpPlat
                                {
                                    HeaderIdGameOver = await ReadUInt16Async(reader),
                                    LocalX = await ReadUInt16Async(reader),
                                    LocalY = await ReadUInt16Async(reader),
                                    HeaderIdFly = await ReadUInt16Async(reader),
                                    GlobalX = await ReadUInt16Async(reader),
                                    GlobalY = await ReadUInt16Async(reader),
                                    IsTeleportPos = await ReadByteAsync(reader),
                                    UnlockOnMapEntry = await ReadByteAsync(reader),
                                    UnlockId = await ReadUInt16Async(reader)
                                };
                                TableDataDpPlat.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while reading the arm9 file: " + ex.Message);
            }
        }

        public async Task PopulateTablesFromDataDpPlatAsync()
        {
            dt_GameOverWarps.Rows.Clear();
            dt_FlyWarps.Rows.Clear();
            dt_UnlockSettings.Rows.Clear();

            foreach (var row in TableDataDpPlat)
            {
                dt_GameOverWarps.Rows.Add(
                    Headers[row.HeaderIdGameOver],
                    row.LocalX,
                    row.LocalY
                );

                dt_FlyWarps.Rows.Add(
                   Headers[row.HeaderIdFly],
                    row.GlobalX,
                    row.GlobalY
                );

                dt_UnlockSettings.Rows.Add(
                    row.IsTeleportPos == 1,
                    row.UnlockOnMapEntry == 1,
                    row.UnlockId
                );
            }

            await Task.CompletedTask;
        }

        public async Task PopulateTablesFromDataHgssAsync()
        {
            dt_GameOverWarps.Rows.Clear();
            dt_FlyWarps.Rows.Clear();
            dt_UnlockSettings.Rows.Clear();

            foreach (var row in TableDataHgss)
            {
                dt_GameOverWarps.Rows.Add(
                    Headers[row.HeaderIdGameOver],
                    row.LocalX,
                    row.LocalY
                );

                dt_FlyWarps.Rows.Add(
                    Headers[row.HeaderIdFly],
                    row.GlobalX,
                    row.GlobalY
                );

                int newRowIndex = dt_UnlockSettings.Rows.Add(
                    Headers[row.HeaderIdUnlockWarp],
                    row.GlobalXUnlock,
                    row.GlobalYUnlock,
                    row.UnlockId
                    );

                DataGridViewRow newRow = dt_UnlockSettings.Rows[newRowIndex];
                DataGridViewComboBoxCell comboBoxCell = (DataGridViewComboBoxCell)newRow.Cells["warpCondition"];

                comboBoxCell.Value = comboBoxCell.Items[row.WarpCondition];
            }

            await Task.CompletedTask;
        }

        private void AddComboBoxColumn(DataGridView dataGridView, string name, string headerText, List<string> dataSource)
        {
            DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                DataSource = dataSource,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            dataGridView.Columns.Add(comboBoxColumn);
        }

        private bool AreAllCellsValid(DataGridView dgv)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.ErrorText != string.Empty)
                    {
                        return false; // Invalid cell found
                    }
                }
            }
            return true;
        }

        private void btn_SaveChanges_Click(object sender, EventArgs e)
        {
            bool hasInvalidCells = false;

            // Validate every non-combo box cell in dt_GameOverWarps
            foreach (DataGridViewRow row in dt_GameOverWarps.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    // Skip validation for ComboBox cells
                    if (!(dt_GameOverWarps.Columns[cell.ColumnIndex] is DataGridViewComboBoxColumn))
                    {
                        if (!ValidateCell(cell))
                        {
                            hasInvalidCells = true;
                            break;
                        }
                    }
                }
            }

            // Validate every non-combo box cell in dt_FlyWarps
            foreach (DataGridViewRow row in dt_FlyWarps.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    // Skip validation for ComboBox cells
                    if (!(dt_FlyWarps.Columns[cell.ColumnIndex] is DataGridViewComboBoxColumn))
                    {
                        if (!ValidateCell(cell))
                        {
                            hasInvalidCells = true;
                            break;
                        }
                    }
                }
            }

            // Validate every non-combo box cell in dt_UnlockSettings
            foreach (DataGridViewRow row in dt_UnlockSettings.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    // Skip validation for ComboBox cells
                    if (!(dt_UnlockSettings.Columns[cell.ColumnIndex] is DataGridViewComboBoxColumn))
                    {
                        if (!ValidateCell(cell))
                        {
                            hasInvalidCells = true;
                            break;
                        }
                    }
                }
            }

            // If no invalid cells, proceed with saving
            if (!hasInvalidCells)
            {
                // Save logic goes here
                MessageBox.Show("Data is valid, proceeding with save...");
            }
            else
            {
                MessageBox.Show("Cannot save! Some fields contain invalid data.");
            }
        }

        private void DataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;

            if (dgv.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn)
            {
                return;
            }

            if (dgv.IsCurrentCellDirty)
            {
                string cellValue = e.FormattedValue.ToString();

                if (!IsValidNumericInput(cellValue))
                {
                    MessageBox.Show("Invalid input! Please enter a numeric value.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    btn_SaveChanges.Enabled = false;

                    e.Cancel = true;
                }
                else
                {
                    btn_SaveChanges.Enabled = true;
                }
            }
        }

        private void FlyEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            isFormClosing = true;
            e.Cancel = false;
        }

        private bool IsValidNumericInput(string input)
        {
            // Check if the input is a valid integer or decimal number
            return decimal.TryParse(input, out _);
        }

        private void PopulateColumns()
        {
            dt_GameOverWarps.Columns.Clear();
            dt_FlyWarps.Columns.Clear();
            dt_UnlockSettings.Columns.Clear();

            dt_GameOverWarps.AllowUserToAddRows = false;
            dt_FlyWarps.AllowUserToAddRows = false;
            dt_UnlockSettings.AllowUserToAddRows = false;

            dt_GameOverWarps.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dt_FlyWarps.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dt_UnlockSettings.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dt_GameOverWarps.ShowRowErrors = true;
            dt_FlyWarps.ShowRowErrors = true;
            dt_UnlockSettings.ShowRowErrors = true;

            dt_GameOverWarps.CellValidating += DataGridView_CellValidating;
            dt_FlyWarps.CellValidating += DataGridView_CellValidating;
            dt_UnlockSettings.CellValidating += DataGridView_CellValidating;

            if (GameFamily == GameFamilies.HGSS)
            {
                AddComboBoxColumn(dt_GameOverWarps, "headerIdGameOver", "Header ID (GameOver)", Headers);
                AddComboBoxColumn(dt_FlyWarps, "headerIdFly", "Header ID (Fly)", Headers);
                AddComboBoxColumn(dt_UnlockSettings, "headerIdUnlock", "Header ID (Unlock)", Headers);

                dt_GameOverWarps.Columns.Add("localX", "Local X");
                dt_GameOverWarps.Columns.Add("localY", "Local Y");

                dt_FlyWarps.Columns.Add("globalX", "Global X");
                dt_FlyWarps.Columns.Add("globalY", "Global Y");

                dt_UnlockSettings.Columns.Add("globalXUnlock", "Global X");
                dt_UnlockSettings.Columns.Add("globalYUnlock", "Global Y");
                dt_UnlockSettings.Columns.Add("unlockId", "Unlock ID");

                DataGridViewComboBoxColumn warpConditionColumn = new DataGridViewComboBoxColumn
                {
                    Name = "warpCondition",
                    HeaderText = "Warp Condition"
                };
                warpConditionColumn.Items.AddRange(0, 1, 2, 3);  // Add values 0-3
                warpConditionColumn.ValueType = typeof(int);
                dt_UnlockSettings.Columns.Add(warpConditionColumn);
            }
            else if (GameFamily == GameFamilies.DP || GameFamily == GameFamilies.Plat)
            {
                AddComboBoxColumn(dt_GameOverWarps, "headerIdGameOver", "Header ID (GameOver)", Headers);
                AddComboBoxColumn(dt_FlyWarps, "headerIdFly", "Header ID (Fly)", Headers);

                dt_GameOverWarps.Columns.Add("localX", "Local X");
                dt_GameOverWarps.Columns.Add("localY", "Local Y");

                dt_FlyWarps.Columns.Add("globalX", "Global X");
                dt_FlyWarps.Columns.Add("globalY", "Global Y");

                DataGridViewCheckBoxColumn isTeleportPosColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "isTeleportPos",
                    HeaderText = "Is Teleport Pos"
                };
                dt_UnlockSettings.Columns.Add(isTeleportPosColumn);

                DataGridViewCheckBoxColumn autoUnlockColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "autoUnlock",
                    HeaderText = "Unlock on Map Entry?"
                };
                dt_UnlockSettings.Columns.Add(autoUnlockColumn);

                dt_UnlockSettings.Columns.Add("unlockId", "Unlock ID");
            }
        }

        private async Task<byte> ReadByteAsync(BinaryReader reader)
        {
            byte[] buffer = new byte[1];
            await reader.BaseStream.ReadAsync(buffer, 0, 1);
            return buffer[0];
        }

        private async Task<ushort> ReadUInt16Async(BinaryReader reader)
        {
            byte[] buffer = new byte[2];
            await reader.BaseStream.ReadAsync(buffer, 0, 2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        // Helper method to validate a cell
        private bool ValidateCell(DataGridViewCell cell)
        {
            // Trigger the same logic as CellValidating
            string cellValue = cell.EditedFormattedValue.ToString();

            if (string.IsNullOrWhiteSpace(cellValue) || !IsValidNumericInput(cellValue))
            {
                // Invalid input
                cell.ErrorText = "Only numeric values are allowed";
                return false;
            }
            else
            {
                // Clear error if input is valid
                cell.ErrorText = string.Empty;
                return true;
            }
        }
    }
}