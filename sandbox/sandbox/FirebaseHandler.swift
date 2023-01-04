

import SwiftUI
import UIKit
import Firebase
import FirebaseAuth
import FirebaseCore
import FirebaseFirestore

class DataHandler: NSObject, ObservableObject {
    
    var uid: String?
    let db = Firestore.firestore()
    
    @ObservedObject static var shared = DataHandler()
    
    override init() {
        super.init()
        self.getUID()
        var apps = FirebaseApp.allApps
        print("swifty")
        print(apps?.description)
        
    }
    
    func getUID() {
        guard let uid = Auth.auth().currentUser?.uid else { return }
        self.uid = uid
        

        
        
    }
    
    func tryStore() {
        let docRef = db.collection("users").document(self.uid ?? "")
        
        print("SWIFT THING")
        
        docRef.getDocument{ (document, error) in
            
            if let document = document, document.exists {
                let dataDescription = document.data().map(String.init(describing:)) ?? "nil"
                print("Document Data: \(dataDescription)")
            } else {
                print("Document does not exist")
                docRef.setData([
                    "username": "test",
                    "created": Timestamp()
                ], merge: true)
            }
        }
    }
    
    
}
