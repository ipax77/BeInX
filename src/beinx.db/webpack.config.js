const path = require('path');
const VersionedModulePlugin = require('../../scripts/versioned-module-plugin.cjs');
const { version } = require('./package.json');
const filename = `beinx-db.bundle.v${version}.js`;

module.exports = {
  mode: 'production',
  entry: './Client/beinx-db.ts',
  experiments: {
    outputModule: true,
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      },
    ],
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
    modules: [path.resolve(__dirname, 'Client'), 'node_modules'],
  },
  output: {
    filename,
    path: path.resolve(__dirname, 'wwwroot/js'),
    clean: { keep: asset => !/^beinx-db\.bundle(?:\.v[^/]+)?\.js(?:\.LICENSE\.txt)?$/.test(asset) },
    library: {
        type: 'module',
    },
  },
  plugins: [new VersionedModulePlugin(
    path.resolve(__dirname, 'JsModule.g.cs'),
    'beinx.db',
    `_content/beinx.db/js/${filename}`,
  )],
};
