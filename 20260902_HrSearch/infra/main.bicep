// 人材検索サンプル用の Azure リソースを一括で作る。
// リソースグループの作成から行うため、サブスクリプションスコープでデプロイする。
//
//   az deployment sub create \
//     --name hrsearch \
//     --location eastus2 \
//     --template-file infra/main.bicep
//
// 作られるもの
//   - リソースグループ
//   - Azure AI Foundry（AIServices）アカウント
//   - チャットモデルのデプロイ（検索文からキーワードを作る用途）
//   - 埋め込みモデルのデプロイ（ベクトル検索用途）

targetScope = 'subscription'

@description('作成するリソースグループ名')
param resourceGroupName string = 'rg-foundry-hrsearch-dev-eus2'

@description('リージョン。使いたいモデルが提供されているかを事前に確認すること')
param location string = 'eastus2'

@description('Azure AI Foundry アカウント名。既定ではサブスクリプションIDから一意な名前を作る')
param accountName string = 'aif-hrsearch-dev-eus2-${uniqueString(subscription().subscriptionId, resourceGroupName)}'

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module models 'models.bicep' = {
  name: 'hrsearch-models'
  scope: rg
  params: {
    accountName: accountName
    location: location
  }
}

@description('アプリの AzureOpenAI:Endpoint に設定する値')
output endpoint string = models.outputs.endpoint

@description('アプリの AzureOpenAI:ChatDeployment に設定する値')
output chatDeployment string = models.outputs.chatDeployment

@description('アプリの AzureOpenAI:EmbeddingDeployment に設定する値')
output embeddingDeployment string = models.outputs.embeddingDeployment

output resourceGroupName string = rg.name
output accountName string = models.outputs.accountName
