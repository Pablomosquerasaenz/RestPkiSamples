using Lacuna.RestPki.Api;
using Lacuna.RestPki.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebForms {

	public partial class CadesSignature : System.Web.UI.Page {

		public string SignatureFilename { get; private set; }
		public PKCertificate SignerCertificate { get; private set; }

		protected void Page_Load(object sender, EventArgs e) {

			if (!IsPostBack) {

				// Get an instance of the CadesSignatureStarter class, responsible for receiving the signature elements and start the
				// signature process
				var signatureStarter = Util.GetRestPkiClient().GetCadesSignatureStarter();

				// Set the file to be signed as a byte array
				signatureStarter.SetContentToSign(Util.GetSampleDocContent());

				// Set the signature policy
				signatureStarter.SetSignaturePolicy(StandardCadesSignaturePolicies.PkiBrazil.AdrBasica);

				// Optionally, set a SecurityContext to be used to determine trust in the certificate chain
				//signatureStarter.SetSecurityContext(StandardSecurityContexts.PkiBrazil);
				// Note: Depending on the signature policy chosen above, setting the security context may be mandatory (this is not
				// the case for ICP-Brasil policies, which will automatically use the PkiBrazil security context if none is passed)

				// Optionally, set whether the content should be encapsulated in the resulting CMS. If this parameter is ommitted,
				// the following rules apply:
				// - If no CmsToSign is given, the resulting CMS will include the content
				// - If a CmsToCoSign is given, the resulting CMS will include the content if and only if the CmsToCoSign also includes the content
				signatureStarter.SetEncapsulateContent(true);

				// Call the StartWithWebPki() method, which initiates the signature. This yields the token, a 43-character
				// case-sensitive URL-safe string, which identifies this signature process. We'll use this value to call the
				// signWithRestPki() method on the Web PKI component (see javascript on the view) and also to complete the signature
				// on the POST action below (this should not be mistaken with the API access token).
				var token = signatureStarter.StartWithWebPki();

				ViewState["Token"] = token;
			}
		}

		protected void SubmitButton_Click(object sender, EventArgs e) {

			// Get an instance of the PadesSignatureFinisher class, responsible for completing the signature process
			var signatureFinisher = Util.GetRestPkiClient().GetCadesSignatureFinisher();

			// Set the token for this signature (rendered in a hidden input field, see the view)
			signatureFinisher.SetToken((string)ViewState["Token"]);

			// Call the Finish() method, which finalizes the signature process and returns the CMS
			var cms = signatureFinisher.Finish();

			// Get information about the certificate used by the user to sign the file. This method must only be called after
			// calling the Finish() method.
			var signerCertificate = signatureFinisher.GetCertificateInfo();

			// At this point, you'd typically store the CMS on your database. For demonstration purposes, we'll
			// store the CMS on the App_Data folder and render a page with a link to download the CMS and with the
			// signer's certificate details.

			var appDataPath = Server.MapPath("~/App_Data");
			if (!Directory.Exists(appDataPath)) {
				Directory.CreateDirectory(appDataPath);
			}
			var id = Guid.NewGuid();
			var filename = id + ".p7s";
			File.WriteAllBytes(Path.Combine(appDataPath, filename), cms);

			this.SignatureFilename = filename;
			this.SignerCertificate = signerCertificate;
			Server.Transfer("CadesSignatureInfo.aspx");
		}
	}
}