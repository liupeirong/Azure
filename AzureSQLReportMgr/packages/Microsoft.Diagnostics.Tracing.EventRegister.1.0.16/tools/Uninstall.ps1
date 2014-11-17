param($installPath, $toolsPath, $package, $project)
 
  # Need to load MSBuild assembly if it's not loaded yet.
  Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

  # Grab the loaded MSBuild project for the project
  # Normalize project path before calling GetLoadedProjects as it performs a string based match
  $msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects([System.IO.Path]::GetFullPath($project.FullName)) | Select-Object -First 1

  # Calculate the Target name: "Ensure" + SimplePackageName + "Imported"
  $dotIndex = $package.Id.LastIndexOf('.')
  if ($dotIndex -le 0) 
  { $targetName = "Ensure" + $package.Id + "Imported"} 
  else 
  { $targetName = "Ensure" + $package.Id.Substring($dotIndex+1) + "Imported" }

  # Grab the target framework version and identifier from the loaded MSBuild project
  $targetFrameworkIdentifier = $msbuild.Xml.Properties | Where-Object { $_.Name.Equals("TargetFrameworkIdentifier") } | Select-Object -First 1
  $targetFrameworkVersion = $msbuild.Xml.Properties | Where-Object { $_.Name.Equals("TargetFrameworkVersion") } | Select-Object -First 1

  # Find all the imports and targets added by this package.
  $itemsToRemove = @()

  # Allow many in case a past package was incorrectly uninstalled
  $itemsToRemove += $msbuild.Xml.Targets | Where-Object { $_.Name -eq $targetName }
  $itemsToRemove += $msbuild.Xml.Imports | Where-Object { $_.Project.EndsWith($package.Id + '.targets') }

  # Remove the elements and save the project
  if ($itemsToRemove -and $itemsToRemove.length)
  {
    foreach ($itemToRemove in $itemsToRemove)
    {
      $msbuild.Xml.RemoveChild($itemToRemove) | out-null
    }
 
    $project.Save()
  }
