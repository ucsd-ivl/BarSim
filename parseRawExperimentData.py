import json, sys, time
import os.path

class HierarchyTree:

    class Node:
        """
        Node class of the HierarchyTree.
        """

        name = ""
        blinkCount = 0
        lookedAtTime = 0
        children = dict()

        def __init__(self, name, blinkCount, lookedAtTime):
            self.name = name
            self.blinkCount = blinkCount
            self.lookedAtTime = lookedAtTime
            self.children = dict()

        def serialize(self):
            root = dict()
            root["name"] = self.name
            root["blinkCount"] = 0
            root["lookedAtTime"] = self.lookedAtTime
            root["children"] = [self.children[key].serialize() for key in self.children.keys()]
            return root

    rootNode = Node("root", 0, 0)

    def __init__(self):
        self.rootNode = self.Node("root", 0, 0)

    def __parsePath(self, path):
        """
        Parse the comma seperated path to a node into a list of node names from root
        """
        return [nodeName.strip() for nodeName in path.split(',')]

    def __insertNodeHelper(self, pathArray, blinkCount, lookedAtTime, curNode):
        """
        Helper recursive function for inserting a node into this hiearchy tree.
        If the desired node already exists, inserting again would overwrite the
        previous value.
        """

        # Check to see if array is empty
        if not pathArray:
            return

        # Check if the current node exists
        if pathArray[0] not in curNode.children.keys():
            curNode.children[pathArray[0]] = self.Node(pathArray[-1], blinkCount, lookedAtTime)

        # Traverse to that node
        self.__insertNodeHelper(pathArray[1:], blinkCount, lookedAtTime, curNode.children[pathArray[0]])

    def insertNode(self, path, blinkCount, lookedAtTime):
        """
        Given the full path to the node, insert that node into this hierarchy
        tree. If that node already exists, inserting again would overwrite the
        previous value.
        """

        pathArray = path if (type(path) == list) else self.__parsePath(path)
        self.__insertNodeHelper(pathArray, blinkCount, lookedAtTime, self.rootNode)

    def readDataFile(self, path):
        """
        Given the path to the data file, attempt to build a HierarchyTree from
        its content.
        """

        if os.path.isfile(path):
            print("Attempting to parse \"" + path + "\"...")
        else:
            print("ERROR: Invalid file specified \"" + path + "\"!")
            return False

        with open(path, "r") as openFile:
            content = openFile.readlines()
            for data in content[1:]:
                dataFields = [field.strip() for field in data.split(',')]
                lookedAtTime = int(dataFields[0])
                dataPath = dataFields[1:]
                self.insertNode(dataPath, 0, lookedAtTime)

        return True

    def saveAsJSON(self, saveToPath):
        """
        Given the current state of the HierarchyTree, convert its content to
        JSON format
        """

        # Calculate the total time and blink count for root
        print("Attempting to save as JSON...")
        self.rootNode.lookedAtTime = 0
        self.rootNode.blinkCount = 0
        for key in self.rootNode.children.keys():
            self.rootNode.lookedAtTime += self.rootNode.children[key].lookedAtTime
            self.rootNode.blinkCount += self.rootNode.children[key].blinkCount

        with open(saveToPath, "w") as outputFile:
            outputFile.write(json.dumps(self.rootNode.serialize(), indent=4, sort_keys=False))

    def __saveAsCsvHelper(self, outputFileIO, totalLevels, curNode, curPath):
        """
        Recursive helper function to convert the current state of the HierarchyTree
        over to CSV format
        """

        # Base case: Current node is at the max depth level
        if len(curPath) == totalLevels:
            outputFileIO.write(str(curNode.lookedAtTime) + ",")
            outputFileIO.write(str(curNode.blinkCount) + ",")
            for label in curPath:
                outputFileIO.write(label + ",")
            outputFileIO.write("\n")

        # Base case: No more children node to make it to max depth level
        elif len(curNode.children) == 0:
            for i in range(len(curPath), totalLevels):
                curPath.append( curPath[-1] )
            self.__saveAsCsvHelper(outputFileIO, totalLevels, curNode, curPath)

        # Else keep going
        else:
            for childName in curNode.children.keys():
                self.__saveAsCsvHelper(outputFileIO, totalLevels, curNode.children[childName], curPath + [childName])

    def saveAsCSV(self, saveToPath, totalLevels=4):
        """
        Given the current state of the HierarchyTree, convert its content to
        CSV format
        """

        print("Attempting to save as CSV...")
        with open(saveToPath, "w") as outputFile:

            # Write out the CSV header to file
            outputFile.write("Duration (ms),Blink Count,")
            for i in range(0, totalLevels):
                outputFile.write("Label Field " + str(i) + ",")
            outputFile.write("\n")

            # Traverse tree and populate csv file
            for childName in self.rootNode.children.keys():
                self.__saveAsCsvHelper(outputFile, totalLevels, self.rootNode.children[childName], [childName])

def convertRawToFriendly(path, directory=None, csvDepth=4):
    """
    Given the path to the raw data, attempt to parse the data and then convert
    it over to a friendly format (CSV and JSON).
    """

    tree = HierarchyTree()
    if tree.readDataFile(path) == False:
        return False
    tree.saveAsCSV(os.path.splitext(path)[0] + ".csv", csvDepth)
    tree.saveAsJSON(os.path.splitext(path)[0] + ".json")
    return True

def convertEntireDirectory(directoryPath="ExperimentData"):

    # Check if current directory is valid
    if not os.path.isdir(directoryPath) or not os.path.exists(directoryPath):
        print("ERROR! Invalid directory: " + os.path.normpath(directoryPath))
        return False

    # Check directory
    for directoryContent in os.listdir(directoryPath):

        # If we found another subdirectory, check it
        fullPath = os.path.join(directoryPath, directoryContent)
        if os.path.isdir(fullPath):
            convertEntireDirectory(fullPath)

        # Else, see if it's a file we should convert
        else:
            if fullPath.endswith(".data"):
                csvfile = os.path.splitext(fullPath)[0] + ".csv"
                jsonFile = os.path.splitext(fullPath)[0] + ".csv"
                rawDataModifiedTime = os.path.getmtime(fullPath)
                csvModifiedTime = 0 if not os.path.exists(csvfile) else os.path.getmtime(csvfile)
                jsonModifiedTime = 0 if not os.path.exists(jsonFile) else os.path.getmtime(jsonFile)
                if (rawDataModifiedTime >= csvModifiedTime) or (rawDataModifiedTime >= jsonModifiedTime):
                    convertRawToFriendly(fullPath)

    return True

if __name__ == "__main__":

    pathToData = input("Specify raw data path: ")
    returnCode = convertRawToFriendly(pathToData) if (pathToData != "") else convertEntireDirectory()
    print("Done.")

    if returnCode == True:
        time.sleep(1)
    else:
        input("\nPress [ENTER] to exit script")
