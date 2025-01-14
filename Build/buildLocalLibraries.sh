#!/bin/bash
# script for building libpalaso, liblcm and chorus libraries locally for debugging FLEx
# Review: Do we also need to delete the nuget files out of packages

libpalaso_dir=""
liblcm_dir=""
chorus_dir=""
libpalaso_net_ver="net462"
liblcm_net_ver="net461"
chorus_net_ver="net462"
mkall_targets_file="mkall.targets"
packages_dir="../packages"

# dotnet pack result version regex
version_regex="\s*Successfully created package.*\.([0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9\-]+)?)\.nupkg"

# Function to display usage information
function display_usage {
	echo "Usage: $0 [OPTIONS]"
	echo "Options:"
	echo "  -p, --libpalaso <directory>   Specify libpalaso directory path and delete specified files, then run 'dotnet pack'"
	echo "  -l, --liblcm <directory>	  Specify liblcm directory path and run 'dotnet pack'"
	echo "  -c, --chorus <directory>	  Specify chorus directory path and delete specified files, then run 'dotnet pack'"
	echo "  -v, --version <version #>	  Set version numbers for the selected library in the mkall.targets and packages.config (does not delete packages or run pack)"
	echo "  -h, --help					  Display this help message"
	exit 1
}

# Function to run 'dotnet pack' in the liblcm directory
function delete_and_pack_liblcm {
	if [ -n "$liblcm_dir" ]; then

		# Check if the specified directory exists
		if [ ! -d "$packages_dir" ]; then
			echo "Error: The specified packages directory does not exist: $packages_dir"
			exit 1
		fi

		if [ "$use_manual_version" == true ]; then
			version_number="$manual_version"
		else
			echo "Deleting files starting with 'SIL.LCModel' in $packages_dir"
			find "$packages_dir" -name 'SIL.LCModel*' -exec rm -f -r {} \;

			echo "Removing liblcm output packages so that dotnet pack will run and output the version"
			(cd "$liblcm_dir/artifacts" && rm *nupkg)

			echo "Running 'dotnet pack' in the liblcm directory: $liblcm_dir"
			pack_output=$(cd "$liblcm_dir" && dotnet pack -c Debug -p:TargetFrameworks=$liblcm_net_ver)

			# Extract version number using regex
			if [[ $pack_output =~ $version_regex ]]; then
				version_number=${BASH_REMATCH[1]}
				echo "Version number extracted from dotnet pack output: $version_number"
			else
				echo "Error: Unable to extract version number from dotnet pack output. (Maybe build failure or nothing needed building?)"
				exit 1
			fi
			copy_pdb_files "$liblcm_dir/artifacts/Debug/$liblcm_net_ver"
		fi

		# Update LcmNugetVersion in mkall.targets
		update_mkall_targets "LcmNugetVersion" "$version_number"

		# Update packages.config with extracted version
		update_packages_config "SIL.LCModel" "$version_number"

	fi
}

# Function to delete specified files in the chorus directory and run 'dotnet pack'
function delete_and_pack_chorus {
	if [ -n "$chorus_dir" ]; then

		# Check if the specified directory exists
		if [ ! -d "$packages_dir" ]; then
			echo "Error: The specified packages directory does not exist: $packages_dir"
			exit 1
		fi
		prefixes=("SIL.Chorus.App" "SIL.Chorus.LibChorus")
		if [ "$use_manual_version" == true ]; then
			version_number="$manual_version"
		else
			echo "Deleting files starting with specified prefix in $packages_dir"

			for prefix in "${prefixes[@]}"; do
				find "$packages_dir" -name "${prefix}*" -exec rm -f -r {} \;
			done

			echo "Removing chorus output packages so that dotnet pack will run and output the version"
			(cd "$chorus_dir/output" && rm *nupkg)

			echo "Running 'dotnet pack' in the chorus directory: $chorus_dir"
			pack_output=$(cd "$chorus_dir" && dotnet pack -c Debug -p:TargetFrameworks=$chorus_net_ver)

			# Extract version number using regex
			if [[ $pack_output =~ $version_regex ]]; then
				version_number=${BASH_REMATCH[1]}
				echo "Version number extracted from dotnet pack output: $version_number"
			else
				echo "Error: Unable to extract version number from dotnet pack output."
				exit 1
			fi
			copy_pdb_files "$chorus_dir/Output/Debug/$chorus_net_ver"
		fi

		# Update ChorusNugetVersion in mkall.targets
		update_mkall_targets "ChorusNugetVersion" "$version_number"
		# Update packages.config with extracted version
		for prefix in "${prefixes[@]}"; do
			update_packages_config "$prefix" "$version_number"
		done
	fi
}

# Function to delete specified files in the libpalaso directory and run 'dotnet pack'
function delete_and_pack_libpalaso {
	if [ -n "$libpalaso_dir" ]; then

		# Check if the specified directory exists
		if [ ! -d "$packages_dir" ]; then
			echo "Error: The specified packages directory does not exist: $packages_dir"
			exit 1
		fi
		prefixes=("SIL.Core" "SIL.Windows" "SIL.DblBundle" "SIL.WritingSystems" "SIL.Dictionary" "SIL.Lift" "SIL.Lexicon" "SIL.Archiving")
		if [ "$use_manual_version" == true ]; then
			version_number="$manual_version"
		else
			echo "Deleting files starting with specified prefixes in $packages_dir"

			for prefix in "${prefixes[@]}"; do
				find "$packages_dir" -name "${prefix}*" -exec rm -f -r {} \;
			done

			echo "Removing palaso output packages so that dotnet pack will run and output the version"
			(cd "$libpalaso_dir/output" && rm *nupkg)

			echo "Running 'dotnet pack' in the libpalaso directory: $libpalaso_dir"
			pack_output=$(cd "$libpalaso_dir" && dotnet pack -c Debug -p:TargetFrameworks=$libpalaso_net_ver)

			# Extract version number using regex
			if [[ $pack_output =~ $version_regex ]]; then
				version_number=${BASH_REMATCH[1]}
				echo "Version number extracted from dotnet pack output: $version_number"
			else
				echo "Error: Unable to extract version number from dotnet pack output."
				exit 1
			fi
			copy_pdb_files "$libpalaso_dir/output/Debug/$libpalaso_net_ver"
		fi

		# Update PalasoNugetVersion in mkall.targets
		update_mkall_targets "PalasoNugetVersion" "$version_number"

		# Update packages.config with extracted version for each prefix
		for prefix in "${prefixes[@]}"; do
			update_packages_config "$prefix" "$version_number"
		done
	fi
}

# Function to update specified element in mkall.targets
function update_mkall_targets {
	local element="$1"
	local version_number="$2"
	if [ -f "$mkall_targets_file" ]; then
		echo "Updating $element in $mkall_targets_file to $version_number"
		sed -i "s/<$element>.*<\/$element>/<${element}>$version_number<\/${element}>/" "$mkall_targets_file"
	else
		echo "Error: $mkall_targets_file not found."
		exit 1
	fi
}
# Function to update packages.config with extracted version for a given package ID prefix
function update_packages_config {
	local id_prefix="$1"
	local version_number="$2"
	local packages_config_file="nuget-common/packages.config"
	if [ -f "$packages_config_file" ]; then
		echo "Updating $packages_config_file with version $version_number for packages with ID starting with $id_prefix"

		# Use sed to modify lines starting with the specified package ID
		sed -i 's/\(package id="'$id_prefix'[\.a-zA-Z0-9]*" \)version="[0-9\.]*[-a-zA-Z0-9]*"/\1version="'$version_number'"/' "$packages_config_file"
	else
		echo "Error: $packages_config_file not found."
		exit 1
	fi
}
# Function to copy .pdb files from artifacts directory to the specified output directory
function copy_pdb_files {
	local artifacts_dir="$1"
	local output_dir="../Output/Debug"
	local downloads_dir="../Downloads"

	# Check if the artifacts directory exists
	if [ ! -d "$artifacts_dir" ]; then
		echo "Error: The specified artifacts directory does not exist: $artifacts_dir"
		exit 1
	fi

	if [ ! -d "$output_dir" ]; then
		echo "Error: The output directory does not exist: $output_dir"
		exit 1
	fi

	if [ ! -d "$downloads_dir" ]; then
		echo "Error: The downloads directory does not exist: $downloads_dir"
		exit 1
	fi

	# Copy .pdb files to the output directory
	find "$artifacts_dir" -name '*.pdb' -exec cp {} "$output_dir" \; -exec cp {} "$downloads_dir" \;

	echo ".pdb files copied from $artifacts_dir to $output_dir and $downloads_dir"
}

# Parse command-line options
while [[ $# -gt 0 ]]; do
	case "$1" in
		-p|--libpalaso)
			libpalaso_dir="$2"
			shift 2
			;;
		-l|--liblcm)
			liblcm_dir="$2"
			shift 2
			;;
		-c|--chorus)
			chorus_dir="$2"
			shift 2
			;;
		-v|--version)
			manual_version="$2"
			use_manual_version=true
			shift 2
			;;
		-h|--help)
			display_usage
			;;
		*)
			echo "Error: Unknown option '$1'"
			display_usage
			;;
	esac
done

# Display usage if no options are provided
if [ -z "$libpalaso_dir" ] && [ -z "$liblcm_dir" ] && [ -z "$chorus_dir" ]; then
	display_usage
fi

mkdir ../Output/Debug
mkdir ../Downloads

# Display the provided directory paths
echo "libpalaso directory: $libpalaso_dir"
echo "liblcm directory: $liblcm_dir"
echo "chorus directory: $chorus_dir"

# Delete specified files in the libpalaso directory and run 'dotnet pack'
delete_and_pack_libpalaso

# Delete specified files in the liblcm directory and run 'dotnet pack'
delete_and_pack_liblcm

# Delete specified files in the chorus directory and run 'dotnet pack'
delete_and_pack_chorus

echo $(date +"%F %T") "Local build and pack finished"
# print a hint for how to use local .pdb files in cyan
tput setaf 6; echo "Build FLEx with /p:UsingLocalLibraryBuild=true to keep the local .pdb files"