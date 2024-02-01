interface AppConfig {
  endpoint: string | undefined;
  masterKey: string | undefined;
  port: string | undefined;
  URLSource: string;
}

const EnviromentApp: AppConfig = {
  endpoint: 'https://192.168.10.116:4040',
  masterKey: 'https://192.168.10.116:4040',
  port: 'https://192.168.10.116:4040',
  URLSource: 'https://192.168.10.116:4040'
}

export default EnviromentApp
