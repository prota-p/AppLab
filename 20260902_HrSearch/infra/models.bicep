// Azure AI Foundry アカウントと、この実験で使う2つのモデルデプロイを作成する。
// リソースグループのスコープで動く。呼び出し元は main.bicep。

@description('Azure AI Foundry（AIServices）アカウント名。グローバルに一意である必要がある')
param accountName string

@description('リージョン')
param location string

@description('チャットモデルのデプロイ名。アプリの ChatDeployment に指定する')
param chatDeploymentName string = 'gpt-56-luna'

@description('チャットモデル名')
param chatModelName string = 'gpt-5.6-luna'

@description('チャットモデルのバージョン')
param chatModelVersion string = '2026-07-09'

@description('チャットモデルのスループット（1000トークン/分 単位）')
param chatCapacity int = 50

@description('埋め込みモデルのデプロイ名。アプリの EmbeddingDeployment に指定する')
param embeddingDeploymentName string = 'text-embedding-3-small'

@description('埋め込みモデル名。次元数1536。変更する場合はアプリ側の vector(1536) も合わせる')
param embeddingModelName string = 'text-embedding-3-small'

@description('埋め込みモデルのバージョン')
param embeddingModelVersion string = '1'

@description('埋め込みモデルのスループット（1000トークン/分 単位）')
param embeddingCapacity int = 50

resource account 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: accountName
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    // https://<name>.openai.azure.com/ 形式のエンドポイントを使うために必須
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: account
  name: chatDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: chatCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
  }
}

resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: account
  name: embeddingDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: embeddingCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: embeddingModelName
      version: embeddingModelVersion
    }
  }
  // 同じアカウントへのモデルデプロイは同時に実行すると競合するため、直列化する
  dependsOn: [
    chatDeployment
  ]
}

output accountName string = account.name
output endpoint string = 'https://${account.name}.openai.azure.com/'
output chatDeployment string = chatDeploymentName
output embeddingDeployment string = embeddingDeploymentName
