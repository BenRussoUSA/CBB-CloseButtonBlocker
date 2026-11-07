# Microsoft signing

CBB's build script supports Azure Artifact Signing, formerly Trusted Signing.
Signing has not been activated for this release. The default artifacts are
unsigned. A configured signing build stops if signing or verification fails.

## Setup and maintenance

This adds account setup and release infrastructure, rather than changes to CBB's
window-blocking logic. The publisher must create an Azure subscription and an
Artifact Signing account, complete identity validation, then create a Public
Trust certificate profile. Individual developers must be in the United States
or Canada. Microsoft's [setup instructions](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)
explain the identity process.

The published Basic price is US $9.99 per month for up to 5,000 signatures, with
additional signatures charged separately. Pricing was checked on November 5,
2026; check the [current pricing](https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-change-sku)
before enrollment. A CBB release signs at least two files: the application EXE
and its MSI.

For an individual certificate, the verified legal name is the publisher
identity. The certificate also shows city, state or province, and country or
region. Microsoft says the email and street address are excluded. This cannot
be assumed to preserve a GitHub-alias-only public identity. Review the
certificate subject preview before enrolling or distributing signed files.
See the [individual validation instructions](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart).

Microsoft manages signing keys and certificate renewal. Maintenance still
includes Azure billing and signer access, plus periodic identity revalidation.
An expired identity validation stops signing until renewed. Keep signing tools
updated and check signatures on each release. Microsoft documents this behavior
in its [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq) and
[certificate management guide](https://learn.microsoft.com/en-us/azure/artifact-signing/concept-certificate-management).

The implementation is small once an account is validated; account onboarding is
the part the publisher must complete personally. Its duration depends on
Microsoft's identity validation. No account has been created and no payment or
identity documents have been submitted by this project workflow.

## Using the build integration

Install Microsoft's Artifact Signing client tools on the build computer and
grant the signing identity the Certificate Profile Signer role. The client tools
have their own runtime requirements. Those tools are not included in CBB's MSI
and are not needed by its users. Follow Microsoft's
[signing integration instructions](https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-signing-integrations).

Create a local metadata file using the account's actual endpoint and profile:

```json
{
  "Endpoint": "https://eus.codesigning.azure.net",
  "CodeSigningAccountName": "YOUR_ACCOUNT_NAME",
  "CertificateProfileName": "YOUR_PUBLIC_TRUST_PROFILE"
}
```

The endpoint must match your account's Azure region. Save the file outside the
source payload, for example `.tools/signing.local.json`. Files ending in
`.local.json` are ignored by Git and excluded from distribution. Authenticate
using the client's supported Azure credential flow; do not put credentials in
the metadata file or repository.

```powershell
./build.ps1 -SigningMetadataPath .tools/signing.local.json `
  -SignToolPath 'C:\PATH\TO\x64\signtool.exe' `
  -SigningDlibPath 'C:\PATH\TO\x64\Azure.CodeSigning.Dlib.dll'
```

The build uses SHA-256 and Microsoft's RFC 3161 timestamp service. It verifies
both signatures and requires timestamps before generating release checksums.
This integration has been reviewed and its unsigned path tested; live signing
requires the publisher's configured account and has not been exercised.

A signature identifies the publisher and protects file integrity. It does not
guarantee that SmartScreen will suppress warnings for a new release. See
[Microsoft's code-signing guidance](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options).
