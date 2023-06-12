using System.IO;
using System.Threading.Tasks;

using ikvm.io;
using eu.europa.esig.dss.spi.x509.aia;
using eu.europa.esig.dss.validation;
using eu.europa.esig.dss.service.ocsp;
using eu.europa.esig.dss.service.crl;
using eu.europa.esig.dss.spi.x509;
using eu.europa.esig.dss.model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// Register (java) non-transitive service locator dependencies...
ikvm.runtime.Startup.addBootClassPathAssembly(typeof(org.slf4j.simple.SimpleLoggerFactory).Assembly);
ikvm.runtime.Startup.addBootClassPathAssembly(typeof(eu.europa.esig.dss.utils.guava.impl.GoogleGuavaUtils).Assembly);
ikvm.runtime.Startup.addBootClassPathAssembly(typeof(eu.europa.esig.dss.pades.PAdESUtils).Assembly);
ikvm.runtime.Startup.addBootClassPathAssembly(typeof(eu.europa.esig.dss.pdf.openpdf.ITextDefaultPdfObjFactory).Assembly);

app.MapPost("/validation", (HttpRequest req) =>
{
	var tcs = new CommonTrustedCertificateSource();
	var cv = new CommonCertificateVerifier();
	var doc = new InMemoryDocument(new InputStreamWrapper(req.BodyReader.AsStream()));
	var validator = SignedDocumentValidator.fromDocument(doc);

	//tcs.importAsTrusted(GetBouncyStore());

	cv.setAIASource(new DefaultAIASource());
	cv.setOcspSource(new OnlineOCSPSource());
	cv.setCrlSource(new OnlineCRLSource());
	cv.addTrustedCertSources(tcs);

	validator.setCertificateVerifier(cv);

	return validator.validateDocument().getXmlValidationReport();
})
.WithName("Validation")
.WithOpenApi();

app.Run();
