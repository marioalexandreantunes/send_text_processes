using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ChildApp
{
    public partial class ChildForm : Form
    {
        // Constants for WM_COPYDATA
        private const int WM_COPYDATA = 0x004A;
        
        // COPYDATASTRUCT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;    // User defined data
            public int cbData;       // Size of data in bytes
            public IntPtr lpData;    // Pointer to data
        }

        // Import SendMessage function from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private IntPtr parentHandle;      // Handle of parent window
        private TextBox inputSend;        // Input field to send data
        private Button btnSend;           // Button to send data
        private System.Windows.Forms.Label lblReceived;  // Label for received data
        private TextBox inputReceived;    // Input field to display received data

        public ChildForm(IntPtr hParent)
        {
            parentHandle = hParent;
            InitializeComponent();
            
            // Send this window's handle to the parent
            SendDataToParent(this.Handle.ToString());
        }

        private void InitializeComponent()
        {
            this.Text = "Child";
            this.Size = new System.Drawing.Size(316, 189);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(440, 100);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Input field for sending data
            inputSend = new TextBox();
            inputSend.Location = new System.Drawing.Point(10, 10);
            inputSend.Size = new System.Drawing.Size(280, 20);
            this.Controls.Add(inputSend);

            // Send button
            btnSend = new Button();
            btnSend.Text = "SEND TO PARENT";
            btnSend.Location = new System.Drawing.Point(80, 35);
            btnSend.Size = new System.Drawing.Size(140, 20);
            btnSend.Click += BtnSend_Click;
            this.Controls.Add(btnSend);

            // Label for received data
            lblReceived = new System.Windows.Forms.Label();
            lblReceived.Text = "RECEIVED FROM PARENT";
            lblReceived.Location = new System.Drawing.Point(10, 75);
            lblReceived.Size = new System.Drawing.Size(280, 20);
            this.Controls.Add(lblReceived);

            // Input field to display received data (read-only)
            inputReceived = new TextBox();
            inputReceived.Location = new System.Drawing.Point(10, 90);
            inputReceived.Size = new System.Drawing.Size(280, 20);
            inputReceived.ReadOnly = true;
            this.Controls.Add(inputReceived);
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            // Send data only if input is not empty
            if (!string.IsNullOrEmpty(inputSend.Text))
            {
                SendDataToParent(inputSend.Text);
            }
        }

        private void SendDataToParent(string data)
        {
            // Check if parent handle is valid
            if (parentHandle != IntPtr.Zero)
            {
                // Convert string to byte array (null-terminated)
                byte[] dataBytes = Encoding.Default.GetBytes(data + "\0");
                
                // Create COPYDATASTRUCT
                COPYDATASTRUCT cds = new COPYDATASTRUCT();
                cds.dwData = IntPtr.Zero;           // User defined data (not used)
                cds.cbData = dataBytes.Length;      // Size of data
                cds.lpData = Marshal.AllocHGlobal(dataBytes.Length);  // Allocate memory for data
                
                // Copy data to allocated memory
                Marshal.Copy(dataBytes, 0, cds.lpData, dataBytes.Length);
                
                // Allocate memory for COPYDATASTRUCT and copy it
                IntPtr cdsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(cds));
                Marshal.StructureToPtr(cds, cdsPtr, false);
                
                // Send message to parent window
                SendMessage(parentHandle, WM_COPYDATA, IntPtr.Zero, cdsPtr);
                
                // Free allocated memory
                Marshal.FreeHGlobal(cds.lpData);
                Marshal.FreeHGlobal(cdsPtr);
            }
        }

        // Override WndProc to handle Windows messages
        protected override void WndProc(ref Message m)
        {
            // Handle WM_COPYDATA message
            if (m.Msg == WM_COPYDATA)
            {
                // Convert lParam to COPYDATASTRUCT
                COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT));
                
                // Copy data from unmanaged memory to managed byte array
                byte[] data = new byte[cds.cbData];
                Marshal.Copy(cds.lpData, data, 0, cds.cbData);
                
                // Convert byte array to string and remove null terminator
                string receivedText = Encoding.Default.GetString(data).TrimEnd('\0');
                
                // Update UI on main thread (thread-safe)
                this.Invoke(new Action(() =>
                {
                    inputReceived.Text = receivedText;
                }));
                
                // Set result and return (message handled)
                m.Result = IntPtr.Zero;
                return;
            }
            
            // Call base WndProc for other messages
            base.WndProc(ref m);
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Check if parent handle was provided
            if (args.Length == 0)
            {
                MessageBox.Show("This program must be executed by Parent.au3", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IntPtr parentHandle;
            
            // Try different parsing methods for compatibility with AutoIt (32-bit vs 64-bit)
            try
            {
                // First try as int (32-bit)
                if (int.TryParse(args[0], out int handle32))
                {
                    parentHandle = new IntPtr(handle32);
                }
                // If that fails, try as long (64-bit)
                else if (long.TryParse(args[0], out long handle64))
                {
                    parentHandle = new IntPtr(handle64);
                }
                // If that fails, try hexadecimal parsing
                else if (args[0].StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    string hexValue = args[0].Substring(2);
                    long handleHex = Convert.ToInt64(hexValue, 16);
                    parentHandle = new IntPtr(handleHex);
                }
                // Last resort: direct conversion
                else
                {
                    long handleValue = Convert.ToInt64(args[0]);
                    parentHandle = new IntPtr(handleValue);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error converting handle: {args[0]}\nError: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validate parent handle
            if (parentHandle == IntPtr.Zero)
            {
                MessageBox.Show("Parent handle is zero", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Initialize Windows Forms application
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            
            // Create and run child form
            ChildForm childForm = new ChildForm(parentHandle);
            System.Windows.Forms.Application.Run(childForm);
        }
    }
}