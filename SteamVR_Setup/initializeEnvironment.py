import sys, os, shutil, filecmp

DEFAULT_STEAM_DIRECTORY = "C:/Program Files (x86)/Steam/"
VR_DEVICE_LOCAL_PATH = "steamapps/common/SteamVR/drivers/null/resources/settings/"
VR_SETTING_LOCAL_PATH = "steamapps/common/SteamVR/resources/settings/"
ACTIVE_SETTING_NAMES = "default.vrsettings"
COPIED_SETTINGS_NAME = "default.original.vrsettings"
REFERENCE_DRIVER_SETTINGS = "drivers.vrsettings"
REFERENCE_SETTINGS_SETTINGS = "settings.vrsettings"
steam_directory = DEFAULT_STEAM_DIRECTORY

# This script was built specifically for python3
if sys.version_info[0] < 3:
    print("Python 3 or a more recent version is required.")
    exit(-1)

print("Default Steam Directory: ", DEFAULT_STEAM_DIRECTORY)
response = input("Use Default Steam Directory? [Y/n]: ").lower()

if((response == "") or (response[0] != "y")):
    steam_directory = input("Please specify correct directory: ")

print("\n=========================================================")
print("Using steam directory: ", steam_directory)
print("=========================================================")

print("\nChecking steam directory... ", end="")
if os.path.isdir(steam_directory):
    print("Success")
else:
    print("FAILED -- INVALID PATH")
    exit(-1)

files_to_change = [{"name" : "device", "path" : os.path.join(steam_directory, VR_DEVICE_LOCAL_PATH), "ref" : REFERENCE_DRIVER_SETTINGS},
                   {"name" : "setting", "path" : os.path.join(steam_directory, VR_SETTING_LOCAL_PATH), "ref" : REFERENCE_SETTINGS_SETTINGS}]

for file in files_to_change:
    print("\nSetting " + file["name"] +  " properties...")
    print("\tChecking " + file["name"] +  " vrsettings...")
    if os.path.isdir(file["path"]) == False:
        print("\t\tDevice directory (" + file["path"] +") does not exist")
        exit(-1)
    print("\tAttempting to change " + file["name"] +  " setting...")
    if os.path.exists(os.path.join(file["path"], ACTIVE_SETTING_NAMES)):
        if os.path.isfile(os.path.join(file["path"], ACTIVE_SETTING_NAMES)):
            if filecmp.cmp(os.path.join(file["path"], ACTIVE_SETTING_NAMES), file["ref"]):
                print("\t\tAlready set. Skipping...")
            else:
                print("\t\t[" + ACTIVE_SETTING_NAMES + "] exists. Making copy as [" + COPIED_SETTINGS_NAME + "]")
                shutil.copyfile(os.path.join(file["path"], ACTIVE_SETTING_NAMES), os.path.join(file["path"], COPIED_SETTINGS_NAME))
        else:
            print("\t\t" + ACTIVE_SETTING_NAMES + " should not be a directory")
            exit(-1)
        shutil.copyfile(file["ref"], os.path.join(file["path"], ACTIVE_SETTING_NAMES))

print("\nSuccess.\n")
