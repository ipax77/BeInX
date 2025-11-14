const path = require('path');

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
    filename: 'beinx-db.bundle.js',
    path: path.resolve(__dirname, 'wwwroot/js'),
    library: {
        type: 'module',
    },
  },
};