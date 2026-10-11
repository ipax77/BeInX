import { AFRelationship, decodePDFRawStream, PDFArray, PDFDict, PDFDocument, PDFHexString, PDFName, PDFRawStream, PDFString } from "pdf-lib";
import { InvoiceDto } from "./dtos/invoice-dto";

export class PdfA3Converter {
    private static readonly invoiceFileNames = ["invoice.xml", "factur-x.xml", "zugferd-invoice.xml"];
    private static iccProfilePromise: Promise<Uint8Array> | undefined;

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
        await this.embedXmlInvoice(doc, xmlInvoice);
        
        return await doc.save();
    }

    protected async loadIccProfile(): Promise<Uint8Array> {
        if (!PdfA3Converter.iccProfilePromise) {
            PdfA3Converter.iccProfilePromise = (async () => {
                const response = await fetch('./_content/BlazorInvoice.Pdf/colorprofiles/sRGB2014.icc');
                if (!response.ok) {
                    throw new Error(`Failed to load ICC profile: HTTP ${response.status}`);
                }
                return new Uint8Array(await response.arrayBuffer());
            })().catch(error => {
                PdfA3Converter.iccProfilePromise = undefined;
                throw error;
            });
        }
        return PdfA3Converter.iccProfilePromise;
    }

    private async embedXmlInvoice(doc: PDFDocument, xml: string): Promise<void> {
        await doc.attach(new TextEncoder().encode(xml), "invoice.xml", {
            mimeType: "application/xml",
            description: "Invoice XML",
            afRelationship: AFRelationship.Alternative,
        });
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
        const sellerName = this.escapeXmlText(invoiceDto.sellerParty.name);
        const invoiceId = this.escapeXmlText(invoiceDto.id);
        const metadataXML = `
            <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="Adobe XMP Core 5.2-c001 63.139439, 2010/09/27-13:37:26        ">
                <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
        
                <rdf:Description rdf:about="" xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:format>application/pdf</dc:format>
                    <dc:creator>
                    <rdf:Seq>
                        <rdf:li>${sellerName}</rdf:li>
                    </rdf:Seq>
                    </dc:creator>
                    <dc:title>
                    <rdf:Alt>
                        <rdf:li xml:lang="x-default">${invoiceId}</rdf:li>
                    </rdf:Alt>
                    </dc:title>
                </rdf:Description>
        
                <rdf:Description rdf:about="" xmlns:xmp="http://ns.adobe.com/xap/1.0/">
                    <xmp:CreatorTool>${sellerName}</xmp:CreatorTool>
                    <xmp:CreateDate>${issueDateUTC}</xmp:CreateDate>
                    <xmp:ModifyDate>${issueDateUTC}</xmp:ModifyDate>
                    <xmp:MetadataDate>${issueDateUTC}</xmp:MetadataDate>
                </rdf:Description>
        
                <rdf:Description rdf:about="" xmlns:pdf="http://ns.adobe.com/pdf/1.3/">
                    <pdf:Producer>${sellerName}</pdf:Producer>
                </rdf:Description>
        
                <rdf:Description rdf:about="" xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
                    <pdfaid:part>3</pdfaid:part>
                    <pdfaid:conformance>B</pdfaid:conformance>
                </rdf:Description>
                </rdf:RDF>
            </x:xmpmeta>
            <?xpacket end="w"?>
            `.trim()

        const metadataStream = doc.context.stream(new TextEncoder().encode(metadataXML), {
        Type: "Metadata",
        Subtype: "XML",
        })
        const metadataStreamRef = doc.context.register(metadataStream)
        doc.catalog.set(PDFName.of("Metadata"), metadataStreamRef)
    }

    private escapeXmlText(value: string): string {
        return value.replace(/[&<>"']/g, character => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&apos;',
        })[character]!);
    }

    private setColorProfile(doc: PDFDocument, iccBuffer: Uint8Array): void {
        const iccStream = doc.context.stream(iccBuffer, {
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

    public async getXmlString(pdfBytes: Uint8Array): Promise<string | null> {
        const doc = await PDFDocument.load(pdfBytes, { updateMetadata: false });
        const files: { name: string; file: PDFDict }[] = [];
        const seenFiles = new Set<PDFDict>();
        const addFile = (file: unknown, treeName?: string) => {
            if (!(file instanceof PDFDict) || seenFiles.has(file)) return;
            const names = [treeName, this.decodeFileName(file.lookup(PDFName.of('UF'))),
                this.decodeFileName(file.lookup(PDFName.of('F')))];
            const name = names.find(value => value !== undefined &&
                PdfA3Converter.invoiceFileNames.includes(value.toLowerCase()));
            if (name !== undefined) {
                seenFiles.add(file);
                files.push({ name: name.toLowerCase(), file });
            }
        };

        const names = doc.catalog.lookup(PDFName.of('Names'));
        const tree = names instanceof PDFDict ? names.lookup(PDFName.of('EmbeddedFiles')) : undefined;
        if (tree instanceof PDFDict) this.visitEmbeddedFiles(tree, addFile, new Set());

        const associatedFiles = doc.catalog.lookup(PDFName.of('AF'));
        if (associatedFiles instanceof PDFArray) {
            for (let index = 0; index < associatedFiles.size(); index++) {
                addFile(associatedFiles.lookup(index));
            }
        }

        // Preserve preference for this application's invoice.xml when several files exist.
        for (const name of PdfA3Converter.invoiceFileNames) {
            for (const candidate of files.filter(candidate => candidate.name === name)) {
                const embedded = candidate.file.lookup(PDFName.of('EF'));
                if (!(embedded instanceof PDFDict)) continue;
                const unicodeStream = embedded.lookup(PDFName.of('UF'));
                const stream = unicodeStream instanceof PDFRawStream
                    ? unicodeStream : embedded.lookup(PDFName.of('F'));
                if (!(stream instanceof PDFRawStream)) continue;
                const bytes = decodePDFRawStream(stream).decode();
                return new TextDecoder('utf-8', { fatal: true }).decode(bytes);
            }
        }
        return null;
    }

    private decodeFileName(value: unknown): string | undefined {
        return value instanceof PDFString || value instanceof PDFHexString ? value.decodeText() : undefined;
    }

    private visitEmbeddedFiles(tree: PDFDict, addFile: (file: unknown, name?: string) => void,
        visited: Set<PDFDict>): void {
        if (visited.has(tree)) return;
        visited.add(tree);
        const names = tree.lookup(PDFName.of('Names'));
        if (names instanceof PDFArray) {
            for (let index = 0; index + 1 < names.size(); index += 2) {
                addFile(names.lookup(index + 1), this.decodeFileName(names.lookup(index)));
            }
        }
        const children = tree.lookup(PDFName.of('Kids'));
        if (children instanceof PDFArray) {
            for (let index = 0; index < children.size(); index++) {
                const child = children.lookup(index);
                if (child instanceof PDFDict) this.visitEmbeddedFiles(child, addFile, visited);
            }
        }
    }
}
