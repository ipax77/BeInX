import { PdfGenerator } from '../pdf-generator';
describe('PdfGenerator', () => {
    it('should sum', () => {
        const generator = new PdfGenerator();
        const sum = generator.sum(1, 2);
        expect(sum).toBe(3);
    });
});
//# sourceMappingURL=pdf-generator-tests.js.map