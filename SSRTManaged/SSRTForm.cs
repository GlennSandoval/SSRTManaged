/********************************************************************************************
** SSRT - Shaun's Simple Ray Tracer                                                       **
**        By Shaun Nirenstein: shaun@cs.uct.ac.za                                         **
**                             http://people.cs.uct.ac.za/~snirenst                       **
**                                                                                        **
** License agreement:                                                                     **
** - This source code (or compiled version thereof) CANNOT be used freely for commercial  **
**   purposes.  If you wish to use it for such purposes, please contact the author,       **
**   Shaun Nirenstein (shaun@cs.uct.ac.za).                                               **
** - For educational purposes (personal or institutional), this source code may be        **
**   modified, distributed or extended freely, as long as this license agreement appears  **
**   at the top of every file as is.                                                      **
** - Any program modified or extended from a version of SSRT (or a modified or extended   **
**   version thereof) must acknowledge that it is SSRT derived in an accessible about box **
**   or credit screen.                                                                    **
**                                                                                        **
********************************************************************************************
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Threading;

namespace SSRTManaged {
	public partial class SSRTForm : Form {

		// SSRT Additions
		Bitmap g_renderBitmap = null;
		byte[][] g_data = null;
		RayTracer tracer;

		public SSRTForm() {
			InitializeComponent();
		}

		private void openToolStripMenuItem_Click( object sender, EventArgs e ) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Scene|*.ssrt";
			DialogResult dr = ofd.ShowDialog();
			if( dr == DialogResult.OK ) {
				Model model = new Model();
				int ret = model.load( ofd.FileName );
				if( ret != 0 ) {
					MessageBox.Show( string.Format( "Could not load file {0}\nError code: {1}", ofd.FileName, ret ) );
					return;
				}
				g_renderBitmap = new Bitmap( model.m_width, model.m_height, PixelFormat.Format24bppRgb );
				pictureBox1.Image = g_renderBitmap;
				g_data = new byte[model.m_width * model.m_height][];
				tracer = new RayTracer( model, g_data );
				DateTime startTime = DateTime.Now;

				new Thread( () => {
					while( tracer.isStillTracing() ) {

						tracer.traceLine();
						tracer.updateBitmap();

						BitmapData bmpData = g_renderBitmap.LockBits(
											 new Rectangle( 0, 0, g_renderBitmap.Width, g_renderBitmap.Height ),
											 ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb );

						byte[] imgdata = new byte[model.m_width * model.m_height * 3];
						int pos = 0;
						for( int i = 0; i < g_data.Length; i++ ) {
							for( int j = 0; j < 3; j++ ) {
								imgdata[pos++] = g_data[i][j];
							}
						}

						System.Runtime.InteropServices.Marshal.Copy( imgdata, 0, bmpData.Scan0, imgdata.Length );

						g_renderBitmap.UnlockBits( bmpData );
						
						if(this.IsDisposed){
							return;
						}

						Invoke( new Action( () => {
							pictureBox1.Refresh();
							var elapsed = DateTime.Now - startTime;
							lblElapsed.Text = elapsed.TotalSeconds.ToString();
						} ) );

					}
				} ).Start();
			}

		}

		private void aboutToolStripMenuItem_Click( object sender, EventArgs e ) {
			new AboutBox1().ShowDialog();
		}
	}
}
