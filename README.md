# ACE7Decrypt v1.1.0
```
This is a command-line application that decrypts Ace Combat 7 PC assets.
Use this for cases in which writing your own code using UAssetAPI or using UAssetGUI's automatic decryption system is not feasible.

Usage: ACE7Decrypt [options ...] <encrypt/decrypt> <input asset> <output asset>
Ensure that the matching uexp file is present in the same directory on use.
NOTE: After encrypting a file, it cannot be renamed. The name of the file is used in the decryption algorithm.

Options:
-Q, --quiet         disables output
-M, --magic         disables file signature check
-R, --recursive     enables operating on files in subfolders
-T, --test          test mode, verifies that algorithm works on provided files
-C, --credits       displays credits

Example:    ACE7Decrypt decrypt plwp_6aam_a0.uasset plwp_6aam_a0_NEW.uasset
            ACE7Decrypt -R encrypt *.umap C:\*.umap
            ACE7Decrypt -Q decrypt *.uasset *_NEW.uasset
```