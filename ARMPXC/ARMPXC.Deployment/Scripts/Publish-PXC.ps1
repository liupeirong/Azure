#Requires -Version 3.0

<#
What else would you like this script to do for you?  Send us feedback here: http://go.microsoft.com/fwlink/?LinkID=517524 
#>

Param(
  [string] $ResourceGroupName = 'pxcrg',
  [string] $Location = 'East US',
  [string] $TemplateFile = '..\Templates\azuredeploy.json',
  [string] $TemplateParametersFile = '..\Templates\azuredeploy.parameters.json'
)

Set-StrictMode -Version 3
Switch-AzureMode AzureResourceManager
$rg=@(Get-AzureResourceGroup | where-object {$_.ResourceGroupName -like $ResourceGroupName})
if (!$rg)
{
    New-AzureResourceGroup -Name $ResourceGroupName -Location $Location
}

# Convert relative paths to absolute paths if needed
$TemplateFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateFile)
$TemplateParametersFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateParametersFile)

# Create or update the resource group using the specified template file and template parameters file
New-AzureResourceGroupDeployment `
                       -ResourceGroupName $ResourceGroupName `
                       -TemplateFile $TemplateFile `
                       -TemplateParameterFile $TemplateParametersFile
