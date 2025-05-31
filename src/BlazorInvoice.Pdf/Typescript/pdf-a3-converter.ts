import { PDFDict, PDFDocument, PDFHexString, PDFName, PDFString } from "pdf-lib";
import { InvoiceDto } from "./dtos/invoice-dto";

export class PdfA3Converter {
    public async createA3Pdf(doc: PDFDocument, invoiceDto: InvoiceDto, culture: string, hexId: string, xmlInvoice: string)
        : Promise<Uint8Array> {
        
        this.setDocumentId(doc, hexId);
        const iccBuffer = await this.loadIccProfile();
        this.setColorProfile(doc, iccBuffer);
        doc.setAuthor(invoiceDto.sellerParty.name)
        doc.setProducer(invoiceDto.sellerParty.name)
        doc.setCreator(invoiceDto.sellerParty.name)
        doc.setTitle(invoiceDto.id)
        doc.setCreationDate(new Date(invoiceDto.issueDate))
        doc.setModificationDate(new Date(invoiceDto.issueDate))
        this.addMetadata(doc, invoiceDto);
        this.setCatalog(doc, culture);
        this.embedXmlInvoice(doc, xmlInvoice);
        this.setCMAPS(doc);
        
        return await doc.save();
    }

    protected async loadIccProfile(): Promise<Uint8Array> {
        const response = await fetch('./_content/Blazorinvoice.Pdf/colorprofiles/sRGB2014.icc');
        const buffer = await response.arrayBuffer();
        return new Uint8Array(buffer);
    }

    private setCMAPS(doc: PDFDocument): void {
        doc.context.enumerateIndirectObjects().forEach(([ref, obj]) => {
            if (obj instanceof PDFDict) {
                const fontEntry = Array.from(obj.entries()).find(([key, value]) => value.toString().includes("Font"))
                if (fontEntry) {
                    obj.set(PDFName.of("CIDToGIDMap"), PDFName.of("Identity"))
                }
            }
        })
    }

    private embedXmlInvoice(doc: PDFDocument, xml: string): void {
        const encoder = new TextEncoder();
        const xmlBuffer = encoder.encode(xml);
        //const xmlBuffer = Buffer.from(xml, "utf8");
        const embeddedFileStream = doc.context.flateStream(xmlBuffer, {
            Type: PDFName.of("EmbeddedFile"),
            Subtype: PDFName.of("application/xml"),
        });

        const embeddedFileRef = doc.context.register(embeddedFileStream);

        const fileSpec = doc.context.obj({
            Type: PDFName.of("Filespec"),
            F: PDFString.of("invoice.xml"),
            UF: PDFString.of("invoice.xml"),
            AFRelationship: PDFName.of("Alternative"),
            Desc: PDFString.of("Invoice XML"),
            EF: {
                F: embeddedFileRef,
                UF: embeddedFileRef,
            },
        });

        const fileSpecRef = doc.context.register(fileSpec);
        doc.catalog.set(PDFName.of("AF"), doc.context.obj([fileSpecRef]));

        const embeddedFilesNameTree = doc.context.obj({
            Names: [PDFString.of("invoice.xml"), fileSpecRef],
        });

        doc.catalog.set(PDFName.of("Names"), doc.context.obj({
            EmbeddedFiles: embeddedFilesNameTree,
        }));
    }

    private setCatalog(doc: PDFDocument, culture: string): void {
        doc.catalog.set(PDFName.of("Version"), PDFName.of("1.7"));
        doc.catalog.set(PDFName.of("PDFExtension"), doc.context.obj({
            Type: PDFName.of("PDFExtension"),
            Extensions: doc.context.obj({
                PDF: doc.context.obj({
                    BaseVersion: PDFName.of("1.7"),
                    ExtensionLevel: 3
                })
            })
        }));
        doc.catalog.set(PDFName.of("Lang"), PDFString.of(culture));
    }

    private addMetadata(doc: PDFDocument, invoiceDto: InvoiceDto): void {
        const date = new Date(invoiceDto.issueDate);
        const issueDateUTC = date.toISOString();
        const metadataXML = `
            <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="Adobe XMP Core 5.2-c001 63.139439, 2010/09/27-13:37:26        ">
                <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
        
                <rdf:Description rdf:about="" xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:format>application/pdf</dc:format>
                    <dc:creator>
                    <rdf:Seq>
                        <rdf:li>${invoiceDto.sellerParty.name}</rdf:li>
                    </rdf:Seq>
                    </dc:creator>
                    <dc:title>
                    <rdf:Alt>
                        <rdf:li xml:lang="x-default">${invoiceDto.id}</rdf:li>
                    </rdf:Alt>
                    </dc:title>
                </rdf:Description>
        
                <rdf:Description rdf:about="" xmlns:xmp="http://ns.adobe.com/xap/1.0/">
                    <xmp:CreatorTool>${invoiceDto.sellerParty.name}</xmp:CreatorTool>
                    <xmp:CreateDate>${issueDateUTC}</xmp:CreateDate>
                    <xmp:ModifyDate>${issueDateUTC}</xmp:ModifyDate>
                    <xmp:MetadataDate>${issueDateUTC}</xmp:MetadataDate>
                </rdf:Description>
        
                <rdf:Description rdf:about="" xmlns:pdf="http://ns.adobe.com/pdf/1.3/">
                    <pdf:Producer>${invoiceDto.sellerParty.name}</pdf:Producer>
                </rdf:Description>
        
                <rdf:Description rdf:about="" xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
                    <pdfaid:part>3</pdfaid:part>
                    <pdfaid:conformance>B</pdfaid:conformance>
                </rdf:Description>
                </rdf:RDF>
            </x:xmpmeta>
            <?xpacket end="w"?>
            `.trim()

        const metadataStream = doc.context.stream(metadataXML, {
        Type: "Metadata",
        Subtype: "XML",
        Length: metadataXML.length,
        })
        const metadataStreamRef = doc.context.register(metadataStream)
        doc.catalog.set(PDFName.of("Metadata"), metadataStreamRef)
    }

    private setColorProfile(doc: PDFDocument, iccBuffer: Uint8Array): void {
        const iccStream = doc.context.stream(iccBuffer, {
        Length: iccBuffer.length,
        N: 3,
        })
        const outputIntent = doc.context.obj({
        Type: "OutputIntent",
        S: "GTS_PDFA1",
        OutputConditionIdentifier: PDFString.of("sRGB"),
        DestOutputProfile: doc.context.register(iccStream),
        })
        const outputIntentRef = doc.context.register(outputIntent)
        doc.catalog.set(PDFName.of("OutputIntents"), doc.context.obj([outputIntentRef]))
    }

    private setDocumentId(doc: PDFDocument, hexId: string): void {
        const hexBuffer = PDFHexString.of(hexId)
        doc.context.trailerInfo.ID = doc.context.obj([hexBuffer, hexBuffer])
    }
}