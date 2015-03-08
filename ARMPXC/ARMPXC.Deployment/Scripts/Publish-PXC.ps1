#Requires -Version 3.0

<#
What else would you like this script to do for you?  Send us feedback here: http://go.microsoft.com/fwlink/?LinkID=517524 
#>

Param(
  [string] $ResourceGroupName = 'PXCRG',
  [string] $TemplateFile = '..\Templates\PXC.json',
  [string] $TemplateParametersFile = '..\Templates\PXC.param.dev.json'
)

Set-StrictMode -Version 3
#New-AzureResourceGroup -Name $ResourceGroupName -Location 'East US'

# Convert relative paths to absolute paths if needed
$TemplateFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateFile)
$TemplateParametersFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateParametersFile)

# Create or update the resource group using the specified template file and template parameters file
Switch-AzureMode AzureResourceManager
New-AzureResourceGroupDeployment `
                       -ResourceGroupName $ResourceGroupName `
                       -TemplateFile $TemplateFile `
                       -TemplateParameterFile $TemplateParametersFile
