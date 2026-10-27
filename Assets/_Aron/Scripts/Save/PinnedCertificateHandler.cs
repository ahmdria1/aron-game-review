// (c) Ahmed Alamrani. All rights reserved.
using System.Security.Cryptography.X509Certificates;
using Aron.Logic.Integrity;
using UnityEngine.Networking;

namespace Aron.Save
{
    /// <summary>
    /// Attach to a UnityWebRequest that talks to our own backend host:
    /// <c>request.certificateHandler = new PinnedCertificateHandler(pins); request.disposeCertificateHandlerOnDispose = true;</c>
    /// Unity Gaming Services endpoints are not pinned (the owner is Unity and certificates rotate); they rely on the system trust store.
    /// </summary>
    public sealed class PinnedCertificateHandler : CertificateHandler
    {
        private readonly CertPins pins;
        public PinnedCertificateHandler(CertPins pins) { this.pins = pins; }

        protected override bool ValidateCertificate(byte[] certificateData)
        {
            try { using (var c = new X509Certificate2(certificateData)) return pins.Accept(c.GetPublicKey()); }
            catch (System.Exception) { return pins.Mode != PinMode.Enforce; }
        }
    }
}
