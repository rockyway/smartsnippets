import UIKit

@objc(KeyboardViewController)
final class KeyboardViewController: UIInputViewController {
    override func viewDidLoad() {
        super.viewDidLoad()
        let button = UIButton(type: .system)
        button.setTitle("Next keyboard", for: .normal)
        button.addTarget(self, action: #selector(advanceToNextInputMode), for: .touchUpInside)
        button.frame = CGRect(x: 12, y: 12, width: 180, height: 44)
        view.addSubview(button)
    }
}
